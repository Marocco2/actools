﻿using System;
using AcTools.Render.Base.Cameras;
using AcTools.Render.Base.TargetTextures;
using AcTools.Utils.Helpers;
using SlimDX;
using SlimDX.Direct3D11;
using Viewport = SlimDX.Direct3D11.Viewport;

namespace AcTools.Render.Base.Shadows {
    public class ShadowsDirectional : IDisposable {
        public const float ClipDistance = 50f;

        public class CameraOrthoShadow : CameraOrtho {
            private readonly CameraOrtho _innerCamera;

            public CameraOrthoShadow SmallerCamera;

            public CameraOrthoShadow() {
                _innerCamera = new CameraOrtho();
                _innerCamera.SetLens(1f);
            }

            public override void LookAt(Vector3 pos, Vector3 target, Vector3 up) {
                base.LookAt(pos, target, up);

                if (!Equals(_innerCamera.FarZ, FarZ)) {
                    _innerCamera.Aspect = Aspect;
                    _innerCamera.FarZ = FarZ;
                    _innerCamera.NearZ = NearZ;
                    _innerCamera.Width = Width * 0.95f;
                    _innerCamera.Height = Height * 0.95f;
                    _innerCamera.SetLens(1f);
                }

                _innerCamera.LookAt(pos, target, up);
            }

            public override void UpdateViewMatrix() {
                base.UpdateViewMatrix();
                _innerCamera.UpdateViewMatrix();
            }

            public override bool Visible(BoundingBox box) {
                return base.Visible(box) && SmallerCamera?._innerCamera.Intersect(box) != FrustrumIntersectionType.Inside;
            }
        }

        public class Split : IDisposable {
            private readonly float _size;

            internal readonly CameraOrthoShadow Camera;
            internal readonly TargetResourceDepthTexture Buffer;

            public float GetShadowDepth(BaseCamera camera) {
                var m = Vector3.Transform(new Vector3(0, 0, _size / 2), camera.Proj);
                return m.Z / m.W;
            }

            public Split(float size) {
                _size = size;
                Buffer = TargetResourceDepthTexture.Create();

                Camera = new CameraOrthoShadow {
                    NearZ = 0.1f,
                    FarZ = ClipDistance * 2f,
                    Width = size,
                    Height = size
                };

                Camera.SetLens(1f);
            }

            public void LookAt(Vector3 direction, Vector3 lookAt) {
                Camera.LookAt(lookAt - ClipDistance * Vector3.Normalize(direction), lookAt, new Vector3(0f, 1f, 0f));
                Camera.UpdateViewMatrix();
                ShadowTransform = Camera.ViewProj * new Matrix {
                    M11 = 0.5f,
                    M22 = -0.5f,
                    M33 = 1.0f,
                    M41 = 0.5f,
                    M42 = 0.5f,
                    M44 = 1.0f
                };
            }

            public Matrix ShadowTransform { get; private set; }

            public ShaderResourceView View => Buffer.View;

            public void Dispose() {
                Buffer.Dispose();
            }
        }

        public readonly Split[] Splits;

        private readonly int _mapSize;
        private readonly Viewport _viewport;

        private RasterizerState _rasterizerState;
        private DepthStencilState _depthStencilState;

        public ShadowsDirectional(int mapSize) {
            _mapSize = mapSize;

            Splits = new [] {
                new Split(5f),
                new Split(15f),
                new Split(50f),
                new Split(200f)
            };

            for (var i = 1; i < Splits.Length; i++) {
                Splits[i].Camera.SmallerCamera = Splits[i - 1].Camera;
            }

            _viewport = new Viewport(0, 0, _mapSize, _mapSize, 0, 1.0f);
        }

        public void Initialize(DeviceContextHolder holder) {
            foreach (var split in Splits) {
                split.Buffer.Resize(holder, _mapSize, _mapSize);
            }

            _rasterizerState = RasterizerState.FromDescription(holder.Device, new RasterizerStateDescription {
                CullMode = CullMode.Front,
                FillMode = FillMode.Solid,
                IsAntialiasedLineEnabled = false,
                IsDepthClipEnabled = true,
                DepthBias = 10000,
                DepthBiasClamp = 0f,
                SlopeScaledDepthBias = 1f
            });

            _depthStencilState = DepthStencilState.FromDescription(holder.Device, new DepthStencilStateDescription {
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Greater,
                IsDepthEnabled = true,
                IsStencilEnabled = false
            });
        }

        public void Update(Vector3 direction, Vector3 lookAt) {
            foreach (var split in Splits) {
                split.LookAt(direction, lookAt);
            }
        }

        public void DrawScene(DeviceContextHolder holder, IShadowsDraw draw) {
            holder.SaveRenderTargetAndViewport();

            holder.DeviceContext.Rasterizer.SetViewports(_viewport);
            holder.DeviceContext.OutputMerger.DepthStencilState = null;
            holder.DeviceContext.OutputMerger.BlendState = null;
            holder.DeviceContext.Rasterizer.State = _rasterizerState;

            foreach (var split in Splits) {
                holder.DeviceContext.OutputMerger.SetTargets(split.Buffer.StencilView);
                holder.DeviceContext.ClearDepthStencilView(split.Buffer.StencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1f, 0);
                draw.DrawSceneForShadows(holder, split.Camera);
                holder.DeviceContext.GenerateMips(split.Buffer.View);
            }

            holder.DeviceContext.Rasterizer.State = null;
            holder.DeviceContext.OutputMerger.DepthStencilState = null;
            holder.RestoreRenderTargetAndViewport();
        }

        public void Dispose() {
            for (var i = 0; i < Splits.Length; i++) {
                DisposeHelper.Dispose(ref Splits[i]);
            }

            DisposeHelper.Dispose(ref _rasterizerState);
            DisposeHelper.Dispose(ref _depthStencilState);
        }
    }
}
