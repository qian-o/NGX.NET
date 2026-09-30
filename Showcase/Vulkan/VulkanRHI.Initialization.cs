using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private readonly VkFrame[] slots = new VkFrame[RenderLayout.FramesInFlight];
    private readonly VkBufferResource[] sceneBuffers = new VkBufferResource[4];
    private readonly List<VkBufferResource> uploads = [];

    protected override void InitializeRendererCore()
    {
        InitializeFrames();
        UploadScene();
        InitializeDescriptors();
        InitializePipelines();
    }

    private void InitializeFrames()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            VkFrame frame = slots[i] = new()
            {
                Api = api,
                Device = device
            };

            frame.Constants = CreateBuffer((ulong)(uniformStride * RenderLayout.UniformSlots), BufferUsageFlags.UniformBufferBit, true);
            frame.Objects = CreateBuffer((ulong)(Resources.Scene.Objects.Length * sizeof(SceneObject)), BufferUsageFlags.StorageBufferBit, true);
            CommandPoolCreateInfo pool = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = queueFamily
            };

            Check(api.CreateCommandPool(device, &pool, null, out CommandPool createdPool), "vkCreateCommandPool");
            frame.Pool = createdPool;
            CommandBufferAllocateInfo allocate = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = frame.Pool,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = 1
            };

            CommandBuffer allocated;
            Check(api.AllocateCommandBuffers(device, &allocate, &allocated), "vkAllocateCommandBuffers");
            frame.Command = allocated;
            FenceCreateInfo fence = new()
            {
                SType = StructureType.FenceCreateInfo,
                Flags = FenceCreateFlags.SignaledBit
            };

            Check(api.CreateFence(device, &fence, null, out Fence createdFence), "vkCreateFence");
            frame.Fence = createdFence;
            SemaphoreCreateInfo semaphore = new() { SType = StructureType.SemaphoreCreateInfo };
            Check(api.CreateSemaphore(device, &semaphore, null, out Semaphore createdSemaphore), "vkCreateSemaphore(render complete)");
            frame.RenderComplete = createdSemaphore;
        }
    }

    private void UploadScene()
    {
        commandBuffer = slots[0].Command;
        CommandBufferBeginInfo begin = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Check(api.BeginCommandBuffer(commandBuffer, &begin), "vkBeginCommandBuffer(upload)");
        recording = true;
        sceneBuffers[0] = StaticBuffer<SceneVertex>(Resources.Scene.Vertices, rayGeometry: true);
        sceneBuffers[1] = StaticBuffer<SceneMaterial>(Resources.Scene.Materials);
        sceneBuffers[2] = StaticBuffer<uint>(Resources.Scene.Texels);
        sceneBuffers[3] = StaticBuffer<TextureDescription>(Resources.Scene.TextureInfo);
        UploadFont();
        MemoryBarrier2 memory = new()
        {
            SType = StructureType.MemoryBarrier2Khr,
            SrcStageMask = PipelineStageFlags2.TransferBit,
            SrcAccessMask = AccessFlags2.TransferWriteBit,
            DstStageMask = PipelineStageFlags2.AllCommandsBit,
            DstAccessMask = AccessFlags2.ShaderReadBit
        };

        DependencyInfo dependency = new()
        {
            SType = StructureType.DependencyInfoKhr,
            MemoryBarrierCount = 1,
            PMemoryBarriers = &memory
        };

        api.CmdPipelineBarrier2(commandBuffer, &dependency);

        if (RayQuerySupported)
        {
            InitializeAccelerationStructures();
        }

        ExecuteUploads();
    }

    private void ExecuteUploads()
    {
        Check(api.EndCommandBuffer(commandBuffer), "vkEndCommandBuffer(upload)");
        recording = false;
        CommandBuffer uploadCommand = commandBuffer;
        SubmitInfo submit = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &uploadCommand
        };

        Check(api.QueueSubmit(queue, 1, &submit, default), "vkQueueSubmit(upload)");
        WaitIdle();

        foreach (VkBufferResource upload in uploads)
        {
            upload.Dispose();
        }

        uploads.Clear();
    }
}
