using System;
using System.Collections.Generic;
using UnityEngine;

namespace Villains.Environment
{
    public static class ShelfProductDisturbance
    {
        private const string ShelfAName = "shelfA_gp";
        private const string ShelfBName = "shelfB_gp";
        private const string ProductGroupPrefix = "produce";
        private const string GroupSuffix = "_gp";

        private static readonly Dictionary<Transform, LocalPose> OriginalPoses = new();

        public static bool TryDisturb(Transform hitTransform, Vector3 maxPositionOffset, Vector3 maxRotationOffset)
        {
            if (!TryResolveProductRoot(hitTransform, out Transform product))
                return false;

            Disturb(product, maxPositionOffset, maxRotationOffset);
            return true;
        }

        public static bool TryRestore(Transform hitTransform)
        {
            if (!TryResolveProductRoot(hitTransform, out Transform product)
                || !OriginalPoses.Remove(product, out LocalPose originalPose))
            {
                return false;
            }

            product.SetLocalPositionAndRotation(originalPose.Position, originalPose.Rotation);
            return true;
        }

        private static void Disturb(Transform product, Vector3 maxPositionOffset, Vector3 maxRotationOffset)
        {
            if (!OriginalPoses.TryGetValue(product, out LocalPose originalPose))
            {
                originalPose = new LocalPose(product.localPosition, product.localRotation);
                OriginalPoses.Add(product, originalPose);
            }

            Vector3 positionOffset = new(
                UnityEngine.Random.Range(-maxPositionOffset.x, maxPositionOffset.x),
                UnityEngine.Random.Range(0f, maxPositionOffset.y),
                UnityEngine.Random.Range(-maxPositionOffset.z, maxPositionOffset.z));

            Vector3 rotationOffset = new(
                UnityEngine.Random.Range(-maxRotationOffset.x, maxRotationOffset.x),
                UnityEngine.Random.Range(-maxRotationOffset.y, maxRotationOffset.y),
                UnityEngine.Random.Range(-maxRotationOffset.z, maxRotationOffset.z));

            product.SetLocalPositionAndRotation(
                originalPose.Position + positionOffset,
                originalPose.Rotation * Quaternion.Euler(rotationOffset));
        }

        private static bool TryResolveProductRoot(Transform hitTransform, out Transform product)
        {
            product = null;

            for (Transform current = hitTransform; current != null && current.parent != null; current = current.parent)
            {
                Transform possibleProductGroup = current.parent;
                if (!IsProductGroup(possibleProductGroup) || !IsAllowedShelf(possibleProductGroup.parent))
                    continue;

                product = current;
                return true;
            }

            return false;
        }

        private static bool IsProductGroup(Transform transform)
        {
            return transform != null
                   && transform.name.StartsWith(ProductGroupPrefix, StringComparison.OrdinalIgnoreCase)
                   && transform.name.EndsWith(GroupSuffix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAllowedShelf(Transform transform)
        {
            return transform != null
                   && (string.Equals(transform.name, ShelfAName, StringComparison.Ordinal)
                       || string.Equals(transform.name, ShelfBName, StringComparison.Ordinal));
        }

        private readonly struct LocalPose
        {
            public LocalPose(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
        }
    }
}
