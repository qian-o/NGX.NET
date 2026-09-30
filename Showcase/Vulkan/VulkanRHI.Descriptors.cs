using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Vulkan;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private DescriptorSetLayout descriptorLayout;
    private DescriptorPool descriptorPool;
    private Sampler sampler;

    private void InitializeDescriptors()
    {
        List<DescriptorSetLayoutBinding> bindings =
        [
            new()
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBufferDynamic,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.All
            }
        ];

        for (uint i = 0; i < RenderLayout.SrvCount; i++)
        {
            if (i == 5 && !RayQuerySupported)
            {
                continue;
            }

            bindings.Add(new()
            {
                Binding = i + 1,
                DescriptorType = i < 5 ? DescriptorType.StorageBuffer : i == 5 ? DescriptorType.AccelerationStructureKhr : DescriptorType.SampledImage,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.All
            });
        }

        for (uint i = 0; i < RenderLayout.UavCount; i++)
        {
            bindings.Add(new()
            {
                Binding = 32 + i,
                DescriptorType = DescriptorType.StorageImage,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.All
            });
        }

        bindings.Add(new()
        {
            Binding = 48,
            DescriptorType = DescriptorType.Sampler,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.All
        });
        DescriptorSetLayoutBinding[] layoutBindings = [.. bindings];

        fixed (DescriptorSetLayoutBinding* pointer = layoutBindings)
        {
            DescriptorSetLayoutCreateInfo create = new()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)layoutBindings.Length,
                PBindings = pointer
            };

            Check(api.CreateDescriptorSetLayout(device, &create, null, out DescriptorSetLayout createdLayout), "vkCreateDescriptorSetLayout");
            descriptorLayout = createdLayout;
        }

        List<DescriptorPoolSize> sizes =
        [
            new(DescriptorType.UniformBufferDynamic, RenderLayout.FramesInFlight),
            new(DescriptorType.StorageBuffer, RenderLayout.FramesInFlight * 5),
            new(DescriptorType.SampledImage, RenderLayout.FramesInFlight * (RenderLayout.SrvCount - 6)),
            new(DescriptorType.StorageImage, RenderLayout.FramesInFlight * RenderLayout.UavCount),
            new(DescriptorType.Sampler, RenderLayout.FramesInFlight)
        ];

        if (RayQuerySupported)
        {
            sizes.Add(new(DescriptorType.AccelerationStructureKhr, RenderLayout.FramesInFlight));
        }

        DescriptorPoolSize[] poolSizes = [.. sizes];

        fixed (DescriptorPoolSize* pointer = poolSizes)
        {
            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                MaxSets = RenderLayout.FramesInFlight,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = pointer
            };

            Check(api.CreateDescriptorPool(device, &poolInfo, null, out DescriptorPool createdPool), "vkCreateDescriptorPool");
            descriptorPool = createdPool;
        }

        DescriptorSetLayout setLayout = descriptorLayout;

        foreach (VkFrame frame in slots)
        {
            DescriptorSetAllocateInfo allocate = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = descriptorPool,
                DescriptorSetCount = 1,
                PSetLayouts = &setLayout
            };

            Check(api.AllocateDescriptorSets(device, in allocate, out frame.Descriptors), "vkAllocateDescriptorSets");
        }

        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MaxLod = 1
        };

        Check(api.CreateSampler(device, &samplerInfo, null, out Sampler createdSampler), "vkCreateSampler");
        sampler = createdSampler;
    }

    public override void UpdateDescriptors()
    {
        for (int index = 0; index < slots.Length; index++)
        {
            VkFrame frame = slots[index];
            void Buffer(uint binding, DescriptorType type, VkBufferResource buffer, ulong range)
            {
                DescriptorBufferInfo info = new()
                {
                    Buffer = buffer.Buffer,
                    Range = range
                };

                WriteDescriptorSet write = new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = frame.Descriptors,
                    DstBinding = binding,
                    DescriptorCount = 1,
                    DescriptorType = type,
                    PBufferInfo = &info
                };

                api.UpdateDescriptorSets(device, 1, &write, 0, null);
            }

            void Texture(uint binding, DescriptorType type, VkTexture texture, ImageLayout layout)
            {
                DescriptorImageInfo info = new()
                {
                    ImageView = texture.View,
                    ImageLayout = layout
                };

                WriteDescriptorSet write = new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = frame.Descriptors,
                    DstBinding = binding,
                    DescriptorCount = 1,
                    DescriptorType = type,
                    PImageInfo = &info
                };

                api.UpdateDescriptorSets(device, 1, &write, 0, null);
            }

            Buffer(0, DescriptorType.UniformBufferDynamic, frame.Constants, (ulong)sizeof(FrameConstants));

            for (uint i = 0; i < 5; i++)
            {
                VkBufferResource buffer = i == 4 ? frame.Objects : sceneBuffers[i];
                Buffer(i + 1, DescriptorType.StorageBuffer, buffer, buffer.Size);
            }

            if (RayQuerySupported)
            {
                AccelerationStructureKHR top = frame.Tlas!.Handle;
                WriteDescriptorSetAccelerationStructureKHR acceleration = new()
                {
                    SType = StructureType.WriteDescriptorSetAccelerationStructureKhr,
                    AccelerationStructureCount = 1,
                    PAccelerationStructures = &top
                };

                WriteDescriptorSet write = new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    PNext = &acceleration,
                    DstSet = frame.Descriptors,
                    DstBinding = 6,
                    DescriptorCount = 1,
                    DescriptorType = DescriptorType.AccelerationStructureKhr
                };

                api.UpdateDescriptorSets(device, 1, &write, 0, null);
            }

            for (ImageSlot slot = 0; slot < ImageSlot.Count; slot++)
            {
                Texture(7 + (uint)slot, DescriptorType.SampledImage, (VkTexture)Resources.Frames[index][(int)slot], ImageLayout.ShaderReadOnlyOptimal);
            }

            int previousFrame = (index + RenderLayout.FramesInFlight - 1) % RenderLayout.FramesInFlight;
            Texture(
                RenderLayout.PreviousExposureSrv + 1,
                DescriptorType.SampledImage,
                (VkTexture)Resources.Frames[previousFrame][(int)ImageSlot.Exposure],
                ImageLayout.ShaderReadOnlyOptimal);
            Texture(RenderLayout.FontSrv + 1, DescriptorType.SampledImage, font, ImageLayout.ShaderReadOnlyOptimal);
            Texture(RenderLayout.LightingSamplesSrv + 1, DescriptorType.SampledImage, (VkTexture)Resources.LightingSamples, ImageLayout.ShaderReadOnlyOptimal);

            for (int i = 0; i < RenderLayout.StorageImages.Length; i++)
            {
                Texture(32 + (uint)i, DescriptorType.StorageImage, (VkTexture)Resources.Frames[index][(int)RenderLayout.StorageImages[i]], ImageLayout.General);
            }

            Texture(32 + RenderLayout.LightingSamplesUav, DescriptorType.StorageImage, (VkTexture)Resources.LightingSamples, ImageLayout.General);
            DescriptorImageInfo samplerInfo = new()
            {
                Sampler = sampler
            };

            WriteDescriptorSet samplerWrite = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = frame.Descriptors,
                DstBinding = 48,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.Sampler,
                PImageInfo = &samplerInfo
            };

            api.UpdateDescriptorSets(device, 1, &samplerWrite, 0, null);
        }
    }
}
