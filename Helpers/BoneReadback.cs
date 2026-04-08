using System;
using System.Reflection;
using Colossal.Entities;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace FirstPersonCameraContinued.Helpers
{
    internal class BoneReadback
    {
        private ComputeBuffer _boneBuffer;
        private ComputeBuffer _localTRSBuffer;
        private bool _bufferLookupDone;
        private bool _readbackPending;
        private float4x4[] _lastReadbackData;
        private float3[] _modelSpacePositions;
        private int[] _parentIndices;
        private bool _hierarchyLoaded;
        private int _hierarchyRetryCount;

        private readonly EntityManager _entityManager;

        public BoneReadback()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private static bool IsBufferValid(ComputeBuffer buffer)
        {
            if (buffer == null)
                return false;
            try
            {
                return buffer.count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void LookupBuffers()
        {
            if (_bufferLookupDone)
                return;

            _bufferLookupDone = true;
            AnimatedSystem animatedSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AnimatedSystem>();
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

            FieldInfo boneField = typeof(AnimatedSystem).GetField("m_BoneBuffer", flags);
            if (boneField != null)
                _boneBuffer = boneField.GetValue(animatedSystem) as ComputeBuffer;

            FieldInfo localTRSField = typeof(AnimatedSystem).GetField("m_LocalTRSBoneBuffer", flags);
            if (localTRSField != null)
                _localTRSBuffer = localTRSField.GetValue(animatedSystem) as ComputeBuffer;
        }

        private void LoadHierarchy(Entity entity)
        {
            _parentIndices = null;

            if (!_entityManager.HasBuffer<Animated>(entity))
                return;

            DynamicBuffer<Animated> animateds = _entityManager.GetBuffer<Animated>(entity, true);
            if (animateds.Length == 0)
                return;

            Animated animated = animateds[0];
            int metaIndex = animated.m_MetaIndex;
            int boneCount = (int)(animated.m_BoneAllocation.End - animated.m_BoneAllocation.Begin);

            AnimatedSystem animatedSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<AnimatedSystem>();
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

            FieldInfo instanceField = typeof(AnimatedSystem).GetField("m_InstanceIndices", flags);
            if (instanceField == null)
                return;

            object instanceList = instanceField.GetValue(animatedSystem);
            if (instanceList == null)
                return;

            NativeList<RestPoseInstance> instances = (NativeList<RestPoseInstance>)instanceList;
            int restPoseIndex = -1;

            for (int i = 0; i < instances.Length; i++)
            {
                if (instances[i].m_MetaIndex == metaIndex)
                {
                    restPoseIndex = instances[i].m_RestPoseIndex;
                    break;
                }
            }

            if (restPoseIndex < 0)
                return;

            FieldInfo animInfoField = typeof(AnimatedSystem).GetField("m_AnimInfoBuffer", flags);
            if (animInfoField == null)
                return;

            ComputeBuffer animInfoBuffer = animInfoField.GetValue(animatedSystem) as ComputeBuffer;
            if (!IsBufferValid(animInfoBuffer) || restPoseIndex >= animInfoBuffer.count)
                return;

            AnimationInfoData[] animInfoData = new AnimationInfoData[1];
            animInfoBuffer.GetData(animInfoData, 0, restPoseIndex, 1);
            int hierarchyOffset = animInfoData[0].m_Hierarchy;
            int infoBoneCount = animInfoData[0].m_BoneCount;

            if (hierarchyOffset < 0)
                return;

            FieldInfo indexField = typeof(AnimatedSystem).GetField("m_IndexBuffer", flags);
            if (indexField == null)
                return;

            ComputeBuffer indexBuffer = indexField.GetValue(animatedSystem) as ComputeBuffer;
            if (!IsBufferValid(indexBuffer))
                return;

            int readCount = Math.Min(infoBoneCount, boneCount);
            if (hierarchyOffset + readCount > indexBuffer.count)
                return;

            _parentIndices = new int[readCount];
            indexBuffer.GetData(_parentIndices, 0, hierarchyOffset, readCount);
            _hierarchyLoaded = true;
        }

        private void ComputeModelSpacePositions()
        {
            if (_lastReadbackData == null || _parentIndices == null)
                return;

            int count = Math.Min(_lastReadbackData.Length, _parentIndices.Length);
            if (_modelSpacePositions == null || _modelSpacePositions.Length != count)
                _modelSpacePositions = new float3[count];

            float4x4[] worldTransforms = new float4x4[count];
            for (int i = 0; i < count; i++)
            {
                float4x4 local = _lastReadbackData[i];
                int parent = _parentIndices[i];

                if (parent >= 0 && parent < count)
                    worldTransforms[i] = math.mul(worldTransforms[parent], local);
                else
                    worldTransforms[i] = local;

                _modelSpacePositions[i] = worldTransforms[i].c3.xyz;
            }
        }

        public void ResetForNewEntity()
        {
            _lastReadbackData = null;
            _modelSpacePositions = null;
            _bufferLookupDone = false;
            _boneBuffer = null;
            _localTRSBuffer = null;
            _parentIndices = null;
            _hierarchyLoaded = false;
            _hierarchyRetryCount = 0;
        }

        public bool TryGetBonePosition(int boneIndex, out float3 position)
        {
            position = float3.zero;

            if (_modelSpacePositions != null && boneIndex >= 0 && boneIndex < _modelSpacePositions.Length)
            {
                position = _modelSpacePositions[boneIndex];
                return !math.any(math.isnan(position));
            }

            if (_lastReadbackData != null && boneIndex >= 0 && boneIndex < _lastReadbackData.Length)
            {
                position = _lastReadbackData[boneIndex].c3.xyz;
                return !math.any(math.isnan(position));
            }

            return false;
        }

        public void Update(Entity entity)
        {
            if (!_entityManager.HasBuffer<Animated>(entity))
                return;

            LookupBuffers();

            if (!_hierarchyLoaded && _hierarchyRetryCount < 60)
            {
                _hierarchyRetryCount++;
                LoadHierarchy(entity);
            }

            DynamicBuffer<Animated> animateds = _entityManager.GetBuffer<Animated>(entity, true);
            if (animateds.Length == 0)
                return;

            Animated animated = animateds[0];
            uint boneStart = animated.m_BoneAllocation.Begin;
            int boneCount = (int)(animated.m_BoneAllocation.End - boneStart);
            if (boneCount <= 0)
                return;

            ComputeBuffer readBuffer = _parentIndices != null && IsBufferValid(_localTRSBuffer) ? _localTRSBuffer : _boneBuffer;
            if (!IsBufferValid(readBuffer))
            {
                _bufferLookupDone = false;
                _boneBuffer = null;
                _localTRSBuffer = null;
                return;
            }

            if ((int)boneStart + boneCount > readBuffer.count)
                return;

            if (!_readbackPending)
            {
                _readbackPending = true;
                AsyncGPUReadback.Request(readBuffer, boneCount * 64, (int)boneStart * 64, OnAsyncReadbackComplete);
            }
        }

        private void OnAsyncReadbackComplete(AsyncGPUReadbackRequest request)
        {
            _readbackPending = false;

            if (request.hasError)
                return;

            NativeArray<float4x4> data = request.GetData<float4x4>();
            _lastReadbackData = data.ToArray();

            if (_parentIndices != null)
                ComputeModelSpacePositions();
        }
    }
}
