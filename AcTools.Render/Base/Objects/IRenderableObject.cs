﻿using System;
using AcTools.Render.Base.Cameras;
using SlimDX;

namespace AcTools.Render.Base.Objects {
    public interface IRenderableObject : IDisposable {
        Matrix ParentMatrix { get; set; }

        bool IsReflectable { get; set; }

        BoundingBox? BoundingBox { get; }

        void Draw(DeviceContextHolder contextHolder, ICamera camera, SpecialRenderMode mode = SpecialRenderMode.Deferred);
    }
}
