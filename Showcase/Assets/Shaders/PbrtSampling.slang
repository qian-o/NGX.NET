// pbrt is Copyright(c) 1998-2020 Matt Pharr, Wenzel Jakob, and Greg Humphreys.
// SPDX-License-Identifier: Apache-2.0
// PBRT-v4 b4ce9687e6c695f5582997c61b0c66cf064bdb4a: util/rng.h,
// util/hash.h, util/math.h and util/lowdiscrepancy.h. See Pbrt.LICENSE.txt.
#ifndef SHOWCASE_PBRT_SAMPLING
#define SHOWCASE_PBRT_SAMPLING

// A uint2 holds a 64-bit unsigned integer, low word first. This preserves PBRT
// PCG/MixBits arithmetic without requiring optional Vulkan shaderInt64 support.
uint2 pbrtAdd64(uint2 a, uint2 b)
{
    uint lo = a.x + b.x;
    return uint2(lo, a.y + b.y + (lo < a.x ? 1u : 0u));
}
uint2 pbrtShift64(uint2 a, uint shift)
{
    if (shift == 0) return a;
    if (shift >= 32) return uint2(a.y >> (shift - 32), 0);
    return uint2((a.x >> shift) | (a.y << (32 - shift)), a.y >> shift);
}
uint2 pbrtMultiply64(uint2 a, uint2 b)
{
    uint a0 = a.x & 65535, a1 = a.x >> 16, b0 = b.x & 65535, b1 = b.x >> 16;
    uint p0 = a0 * b0, p1 = a0 * b1, p2 = a1 * b0;
    uint middle = (p0 >> 16) + (p1 & 65535) + (p2 & 65535);
    uint high = a1 * b1 + (p1 >> 16) + (p2 >> 16) + (middle >> 16);
    return uint2(a.x * b.x, high + a.x * b.y + a.y * b.x);
}
uint2 pbrtMixBits(uint2 v)
{
    v ^= pbrtShift64(v, 31);
    v = pbrtMultiply64(v, uint2(0x728ea185, 0x7fb5d329));
    v ^= pbrtShift64(v, 27);
    v = pbrtMultiply64(v, uint2(0xbc2dd44d, 0x81dadef4));
    return v ^ pbrtShift64(v, 33);
}
uint2 pbrtHashStep(uint2 h, uint2 k)
{
    const uint2 multiplier = uint2(0x5bd1e995, 0xc6a4a793);
    k = pbrtMultiply64(k, multiplier);
    k ^= pbrtShift64(k, 47);
    k = pbrtMultiply64(k, multiplier);
    return pbrtMultiply64(h ^ k, multiplier);
}
uint2 pbrtHashFinish(uint2 h)
{
    h ^= pbrtShift64(h, 47);
    h = pbrtMultiply64(h, uint2(0x5bd1e995, 0xc6a4a793));
    return h ^ pbrtShift64(h, 47);
}
uint2 pbrtHash(float3 v)
{
    const uint2 multiplier = uint2(0x5bd1e995, 0xc6a4a793);
    uint2 h = pbrtMultiply64(uint2(12, 0), multiplier);
    h = pbrtHashStep(h, asuint(v.xy));
    h = pbrtMultiply64(h ^ uint2(asuint(v.z), 0), multiplier);
    return pbrtHashFinish(h);
}
struct PbrtRng { uint2 state; uint2 increment; };
uint rngUint(inout PbrtRng rng)
{
    uint2 old = rng.state;
    rng.state = pbrtAdd64(pbrtMultiply64(old, uint2(0x4c957f2d, 0x5851f42d)), rng.increment);
    uint shifted = pbrtShift64(pbrtShift64(old, 18) ^ old, 27).x;
    uint rotation = old.y >> 27;
    return (shifted >> rotation) | (shifted << ((~rotation + 1u) & 31));
}
PbrtRng makePbrtRng(uint2 sequence, uint2 seed)
{
    PbrtRng rng;
    rng.state = 0;
    rng.increment = uint2((sequence.x << 1) | 1, (sequence.y << 1) | (sequence.x >> 31));
    rngUint(rng);
    rng.state = pbrtAdd64(rng.state, seed);
    rngUint(rng);
    return rng;
}
float rng1D(inout PbrtRng rng)
{
    return min(0.9999999403953552, float(rngUint(rng)) * 2.3283064365386963e-10);
}
float2 rng2D(inout PbrtRng rng)
{
    float x = rng1D(rng), y = rng1D(rng);
    return float2(x, y);
}
uint permutationElement(uint i, uint length, uint seed)
{
    uint w = length - 1;
    w |= w >> 1; w |= w >> 2; w |= w >> 4; w |= w >> 8; w |= w >> 16;
    do
    {
        i ^= seed; i *= 0xe170893d; i ^= seed >> 16; i ^= (i & w) >> 4;
        i ^= seed >> 8; i *= 0x0929eb3f; i ^= seed >> 23; i ^= (i & w) >> 1;
        i *= 1 | (seed >> 27); i *= 0x6935fa69; i ^= (i & w) >> 11;
        i *= 0x74dcb303; i ^= (i & w) >> 2; i *= 0x9e501cc3;
        i ^= (i & w) >> 2; i *= 0xc860a3df; i &= w; i ^= i >> 5;
    } while (i >= length);
    return (i + seed) % length;
}
float owenScrambledRadicalInverse(uint base, uint index, uint seed)
{
    float inverseBase = 1.0 / base, inverseBasePower = 1;
    uint2 reversedDigits = 0;
    // This is PBRT's nested Owen scrambling. Each reversed digit prefix hashes
    // the next permutation; including trailing zero digits removes the origin.
    // IEEE float has 24 significand bits: at most 25 base-2 digits survive the
    // rounded 1-inverseBasePower test. The explicit bound also keeps fast-math
    // reassociation from extending the loop until underflow; larger bases stop
    // earlier. This table therefore cannot overflow reversedDigits.
    [loop]
    for (uint digitIndex = 0; digitIndex <= 24 && 1 - inverseBasePower < 1; digitIndex++)
    {
        uint next = index / base;
        uint digit = index - next * base;
        uint digitHash = pbrtMixBits(reversedDigits ^ uint2(seed, 0)).x;
        digit = permutationElement(digit, base, digitHash);
        reversedDigits = pbrtAdd64(pbrtMultiply64(reversedDigits, uint2(base, 0)), uint2(digit, 0));
        inverseBasePower *= inverseBase;
        index = next;
    }
    float reversed = float(reversedDigits.y) * 4294967296.0 + float(reversedDigits.x);
    return min(inverseBasePower * reversed, 0.9999999403953552);
}
static const uint haltonPrimes[128] = {
    2,3,5,7,11,13,17,19,23,29,31,37,41,43,47,53,
    59,61,67,71,73,79,83,89,97,101,103,107,109,113,127,131,
    137,139,149,151,157,163,167,173,179,181,191,193,197,199,211,223,
    227,229,233,239,241,251,257,263,269,271,277,281,283,293,307,311,
    313,317,331,337,347,349,353,359,367,373,379,383,389,397,401,409,
    419,421,431,433,439,443,449,457,461,463,467,479,487,491,499,503,
    509,521,523,541,547,557,563,569,571,577,587,593,599,601,607,613,
    617,619,631,641,643,647,653,659,661,673,677,683,691,701,709,719
};
struct PathSampler { uint pixelSeed; uint index; uint dimension; };
PathSampler makeSampler(uint2 pixel, uint sampleIndex)
{
    PathSampler sampler;
    sampler.pixelSeed = pbrtMixBits(pixel).x;
    sampler.index = sampleIndex;
    sampler.dimension = 0;
    return sampler;
}
float next1D(inout PathSampler sampler)
{
    uint dimension = sampler.dimension++;
    uint seed = pbrtMixBits(uint2(sampler.pixelSeed, dimension)).x;
    // One independent scramble per pixel and dimension. 128 prime dimensions
    // cover the configured path depth; later dimensions restart bases with new
    // scrambles, as PBRT Halton restarts its finite prime table.
    return owenScrambledRadicalInverse(haltonPrimes[dimension % 128], sampler.index, seed);
}
float2 next2D(inout PathSampler sampler)
{
    float x = next1D(sampler), y = next1D(sampler);
    return float2(x, y);
}
#endif
