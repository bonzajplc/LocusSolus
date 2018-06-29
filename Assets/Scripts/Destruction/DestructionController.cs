using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace DestructionEffect
{
    class DestructionSensor
    {
		public Vector3 posLS = Vector3.zero;
        public Bounds AABB;

        public float radius = 0.25f;
        public float innerRadius = 0.8f;
    };

    class DestructionShapeParams
    {
		public Vector3 scale = Vector3.one;
		public Vector3 scaleInv = Vector3.one;
		public Vector3 extrude = new Vector3( 0.1f, 0.1f, 0.1f );

        public float voxelRadius = 0.05f;
    };
}//