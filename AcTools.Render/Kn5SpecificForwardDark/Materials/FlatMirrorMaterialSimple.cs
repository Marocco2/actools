﻿using AcTools.Render.Base;
using AcTools.Render.Base.Cameras;
using AcTools.Render.Base.Materials;
using AcTools.Render.Base.Objects;
using AcTools.Render.Base.Utils;
using AcTools.Render.Shaders;
using SlimDX;

namespace AcTools.Render.Kn5SpecificForwardDark.Materials {
    public class FlatMirrorMaterialSimple : IRenderableMaterial {
        private readonly bool _opaqueMode;
        private EffectDarkMaterial _effect;

        internal FlatMirrorMaterialSimple(bool opaqueMode) {
            _opaqueMode = opaqueMode;
        }

        public void Initialize(IDeviceContextHolder contextHolder) {
            _effect = contextHolder.GetEffect<EffectDarkMaterial>();
        }

        public bool Prepare(IDeviceContextHolder contextHolder, SpecialRenderMode mode) {
            if (mode != SpecialRenderMode.Simple) return false;
            
            contextHolder.DeviceContext.InputAssembler.InputLayout = _effect.LayoutPT;

            if (!_opaqueMode) {
                contextHolder.DeviceContext.OutputMerger.BlendState = contextHolder.States.TransparentBlendState;
                contextHolder.DeviceContext.OutputMerger.DepthStencilState = contextHolder.States.LessEqualReadOnlyDepthState;
            } else {
                contextHolder.DeviceContext.OutputMerger.BlendState = null;
                contextHolder.DeviceContext.OutputMerger.DepthStencilState = null;
            }

            return true;
        }

        public void SetMatrices(Matrix objectTransform, ICamera camera) {
            _effect.FxWorldViewProj.SetMatrix(objectTransform * camera.ViewProj);
            _effect.FxWorld.SetMatrix(objectTransform);
        }

        public void Draw(IDeviceContextHolder contextHolder, int indices, SpecialRenderMode mode) {
            (_opaqueMode ? _effect.TechFlatBackgroundGround : _effect.TechFlatMirror).DrawAllPasses(contextHolder.DeviceContext, indices);

            if (!_opaqueMode) {
                contextHolder.DeviceContext.OutputMerger.BlendState = null;
                contextHolder.DeviceContext.OutputMerger.DepthStencilState = null;
            }
        }

        public bool IsBlending => !_opaqueMode;

        public void Dispose() {}
    }
}