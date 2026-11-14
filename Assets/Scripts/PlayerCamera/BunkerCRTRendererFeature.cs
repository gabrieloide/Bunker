using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP Renderer Feature that composites UI Toolkit panels (GameUIPanel, UIManager HUD,
/// and RetroCRTUIBridge menus) onto the camera color target and applies the RetroCRT pass.
/// Requires Compatibility Mode (m_EnableRenderGraph: 0) in URP Global Settings for WebGL.
/// </summary>
public class BunkerCRTRendererFeature : ScriptableRendererFeature
{
    class BunkerCRTPass : ScriptableRenderPass
    {
        static readonly int TempAId = Shader.PropertyToID("_BunkerCRT_TempA");
        static readonly int TempBId = Shader.PropertyToID("_BunkerCRT_TempB");
        static readonly int OverlayTexId = Shader.PropertyToID("_OverlayTex");

        Material compositeMat;

        public BunkerCRTPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            ConfigureInput(ScriptableRenderPassInput.Color);
        }

        Material GetCompositeMaterial()
        {
            if (compositeMat == null)
            {
                var shader = Shader.Find("Hidden/AlphaComposite");
                if (shader != null && shader.isSupported)
                {
                    compositeMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
            }
            return compositeMat;
        }

        public void Dispose()
        {
            if (compositeMat != null)
            {
                if (Application.isPlaying) Object.Destroy(compositeMat);
                else Object.DestroyImmediate(compositeMat);
                compositeMat = null;
            }
        }

#pragma warning disable CS0618
        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!Application.isPlaying) return;

            var camData = renderingData.cameraData;
            if (camData.cameraType != CameraType.Game) return;

            Camera cam = camData.camera;
            if (cam == null || cam.targetTexture != null) return;

            var crtFilter = cam.GetComponent<RetroCRTFilter>();
            Material crtMat = crtFilter != null ? crtFilter.PrepareMaterial() : null;
            bool hasCrt = crtMat != null && crtMat.shader != null && crtMat.shader.isSupported;

            Material compMat = GetCompositeMaterial();
            bool hasComp = compMat != null;

            Texture gameUiTex = GameUIPanel.Instance != null ? GameUIPanel.Instance.TargetTexture : null;
            Texture hudTex = UIManager.instance != null ? UIManager.instance.HudTarget : null;

            int bridgeCount = RetroCRTUIBridge.Instance != null ? RetroCRTUIBridge.Instance.ActiveTextureCount : 0;

            if (!hasCrt && (!hasComp || (gameUiTex == null && hudTex == null && bridgeCount == 0)))
                return;

            CommandBuffer cmd = CommandBufferPool.Get("BunkerCRT");

            RenderTextureDescriptor desc = camData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            cmd.GetTemporaryRT(TempAId, desc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(TempBId, desc, FilterMode.Bilinear);

            RTHandle colorTarget = camData.renderer.cameraColorTargetHandle;
            RenderTargetIdentifier current = colorTarget;
            RenderTargetIdentifier tempA = new RenderTargetIdentifier(TempAId);
            RenderTargetIdentifier tempB = new RenderTargetIdentifier(TempBId);

            if (hasComp)
            {
                if (gameUiTex != null)
                {
                    RenderTargetIdentifier dst = current == tempA ? tempB : tempA;
                    cmd.SetGlobalTexture(OverlayTexId, gameUiTex);
                    cmd.Blit(current, dst, compMat);
                    current = dst;
                }

                if (hudTex != null)
                {
                    RenderTargetIdentifier dst = current == tempA ? tempB : tempA;
                    cmd.SetGlobalTexture(OverlayTexId, hudTex);
                    cmd.Blit(current, dst, compMat);
                    current = dst;
                }

                if (bridgeCount > 0 && RetroCRTUIBridge.Instance != null)
                {
                    for (int i = 0; i < bridgeCount; i++)
                    {
                        Texture panelTex = RetroCRTUIBridge.Instance.GetActiveTexture(i);
                        if (panelTex == null) continue;
                        RenderTargetIdentifier dst = current == tempA ? tempB : tempA;
                        cmd.SetGlobalTexture(OverlayTexId, panelTex);
                        cmd.Blit(current, dst, compMat);
                        current = dst;
                    }
                }
            }

            if (hasCrt)
            {
                if (current == colorTarget)
                {
                    cmd.Blit(colorTarget, tempA);
                    cmd.Blit(tempA, colorTarget, crtMat);
                }
                else
                {
                    cmd.Blit(current, colorTarget, crtMat);
                }
            }
            else if (current != colorTarget)
            {
                cmd.Blit(current, colorTarget);
            }

            cmd.ReleaseTemporaryRT(TempAId);
            cmd.ReleaseTemporaryRT(TempBId);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#pragma warning restore CS0618
    }

    BunkerCRTPass pass;

    public override void Create()
    {
        pass = new BunkerCRTPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass != null)
            renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        pass = null;
    }
}
