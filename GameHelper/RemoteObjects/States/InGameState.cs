﻿// <copyright file="InGameState.cs" company="None">
// Copyright (c) None. All rights reserved.
// </copyright>

namespace GameHelper.RemoteObjects.States
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Coroutine;
    using CoroutineEvents;
    using GameOffsets.Natives;
    using GameOffsets.Objects.States;
    using ImGuiNET;
    using InGameStateObjects;
    using UiElement;

    /// <summary>
    ///     Reads InGameState Game Object.
    /// </summary>
    public class InGameState : RemoteObjectBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InGameState" /> class.
        /// </summary>
        /// <param name="address">address of the remote memory object.</param>
        internal InGameState(IntPtr address)
            : base(address)
        {
            Core.CoroutinesRegistrar.Add(CoroutineHandler.Start(
                this.OnPerFrame(), "[InGameState] Update Game State", int.MaxValue - 2));
        }

        /// <summary>
        ///     Gets the data related to the current area instance.
        /// </summary>
        public AreaInstance CurrentAreaInstance { get; }

            = new(IntPtr.Zero);

        /// <summary>
        ///     Gets the World to Screen Matrix.
        /// </summary>
        public Matrix4x4 WorldToScreenMatrix { get; private set; }

            = Matrix4x4.Identity;

        /// <summary>
        ///     Gets the UiRoot main child which contains all the UiElements of the game.
        /// </summary>
        public ImportantUiElements GameUi { get; }

            = new(IntPtr.Zero);

        /// <summary>
        ///     Gets the data related to the root ui element.
        /// </summary>
        internal UiElementBase UiRoot { get; }

            = new(IntPtr.Zero);

        /// <summary>
        ///     Converts the World position to Screen location.
        /// </summary>
        /// <param name="worldPosition">3D world position of the entity.</param>
        /// <returns>screen location of the entity.</returns>
        public Vector2 WorldToScreen(StdTuple3D<float> worldPosition)
        {
            var result = Vector2.Zero;
            if (this.Address == IntPtr.Zero)
            {
                return result;
            }

            Vector4 temp0 = new(worldPosition.X, worldPosition.Y, worldPosition.Z, 1f);
            temp0 = Vector4.Transform(temp0, this.WorldToScreenMatrix);
            temp0 /= temp0.W;
            result.X = (temp0.X + 1f) * (Core.Process.WindowArea.Width / 2);
            result.Y = (1.0f - temp0.Y) * (Core.Process.WindowArea.Height / 2);
            return result;
        }

        /// <summary>
        ///     Converts the <see cref="InGameState" /> class data to ImGui.
        /// </summary>
        internal override void ToImGui()
        {
            base.ToImGui();
            if (ImGui.TreeNode("WindowToScreenMatrix"))
            {
                var d = this.WorldToScreenMatrix;
                ImGui.Text($"{d.M11:0.00}\t{d.M12:0.00}\t{d.M13:0.00}\t{d.M14:0.00}");
                ImGui.Text($"{d.M21:0.00}\t{d.M22:0.00}\t{d.M23:0.00}\t{d.M24:0.00}");
                ImGui.Text($"{d.M31:0.00}\t{d.M32:0.00}\t{d.M33:0.00}\t{d.M34:0.00}");
                ImGui.Text($"{d.M41:0.00}\t{d.M42:0.00}\t{d.M43:0.00}\t{d.M44:0.00}");
                ImGui.TreePop();
            }
        }

        /// <inheritdoc />
        protected override void CleanUpData()
        {
            this.CurrentAreaInstance.Address = IntPtr.Zero;
            this.UiRoot.Address = IntPtr.Zero;
            this.GameUi.Address = IntPtr.Zero;
            this.WorldToScreenMatrix = Matrix4x4.Identity;
        }

        /// <inheritdoc />
        protected override void UpdateData(bool hasAddressChanged)
        {
            var reader = Core.Process.Handle;
            var data = reader.ReadMemory<InGameStateOffset>(this.Address);
            this.CurrentAreaInstance.Address = data.LocalData;
            this.UiRoot.Address = data.UiRootPtr;
            this.GameUi.Address = data.IngameUi;
            if (this.WorldToScreenMatrix != data.CameraData.WorldToScreenMatrix)
            {
                this.WorldToScreenMatrix = data.CameraData.WorldToScreenMatrix;
            }
        }

        private IEnumerator<Wait> OnPerFrame()
        {
            while (true)
            {
                yield return new Wait(GameHelperEvents.PerFrameDataUpdate);
                if (this.Address != IntPtr.Zero)
                {
                    this.UpdateData(false);
                }
            }
        }
    }
}