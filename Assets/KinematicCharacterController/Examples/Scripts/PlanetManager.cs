
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using System;

namespace KinematicCharacterController.Examples
{
    public class PlanetManager : MonoBehaviour, IMoverController
    {
        public PhysicsMover PlanetMover;
        public SphereCollider GravityField;
        public float GravityStrength = 10;
        public Vector3 OrbitAxis = Vector3.forward;
        public float OrbitSpeed = 10;

        public List<Teleporter> OnEnterPlanetTeleportingZones;
        public List<Teleporter> OnExitPlanetTeleportingZones;

        private List<PlayerCharacterController> _characterControllersOnPlanet = new List<PlayerCharacterController>();
        private Vector3 _savedGravity;
        private Quaternion _lastRotation;

        private void Start()
        {
            foreach (Teleporter _teleporter in OnEnterPlanetTeleportingZones)
            {
                _teleporter.OnCharacterTeleport -= ControlGravity;
                _teleporter.OnCharacterTeleport += ControlGravity;
            }

            foreach (Teleporter _teleporter in OnExitPlanetTeleportingZones)
            {
                _teleporter.OnCharacterTeleport -= UnControlGravity;
                _teleporter.OnCharacterTeleport += UnControlGravity;
            }

            _lastRotation = SafeQuaternion(PlanetMover.transform.rotation, Quaternion.identity);

            PlanetMover.MoverController = this;
        }

        public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
        {
            goalPosition = PlanetMover.Rigidbody.position;

            // Safety
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                goalRotation = SafeQuaternion(_lastRotation, PlanetMover.transform.rotation);
                return;
            }

            Vector3 safeAxis = OrbitAxis;

            if (
                safeAxis.sqrMagnitude < 0.000001f ||
                float.IsNaN(safeAxis.x) || float.IsNaN(safeAxis.y) || float.IsNaN(safeAxis.z) ||
                float.IsInfinity(safeAxis.x) || float.IsInfinity(safeAxis.y) || float.IsInfinity(safeAxis.z)
            )
            {
                safeAxis = Vector3.forward;
            }

            safeAxis.Normalize();

            Quaternion deltaRotation = Quaternion.AngleAxis(OrbitSpeed * deltaTime, safeAxis);

            Quaternion targetRotation = deltaRotation * _lastRotation;
            targetRotation = SafeQuaternion(targetRotation, PlanetMover.transform.rotation);

            goalRotation = targetRotation;
            _lastRotation = targetRotation;

            // Apply gravity to characters
            foreach (PlayerCharacterController cc in _characterControllersOnPlanet)
            {
                if (!cc)
                {
                    continue;
                }

                Vector3 gravityDirection = PlanetMover.transform.position - cc.transform.position;

                if (gravityDirection.sqrMagnitude > 0.000001f)
                {
                    cc.Gravity = gravityDirection.normalized * GravityStrength;
                }
            }
        }

        void ControlGravity(PlayerCharacterController cc)
        {
            _characterControllersOnPlanet.Add(cc);
        }

        void UnControlGravity(PlayerCharacterController cc)
        {
            cc.Gravity = cc.DefaultGravity;
            _characterControllersOnPlanet.Remove(cc);
        }

        


        private void OnTriggerEnter(Collider other)
        {
            PlayerCharacterController cc = other.GetComponent<PlayerCharacterController>();
            if (cc)
            {
                ControlGravity(cc);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerCharacterController cc = other.GetComponent<PlayerCharacterController>();
            if (cc)
            {
                UnControlGravity(cc);
            }
        }
        private Quaternion SafeQuaternion(Quaternion rotation, Quaternion fallback)
        {
            float sqrMagnitude =
                rotation.x * rotation.x +
                rotation.y * rotation.y +
                rotation.z * rotation.z +
                rotation.w * rotation.w;

            if (
                sqrMagnitude < 0.000001f ||
                float.IsNaN(sqrMagnitude) ||
                float.IsInfinity(sqrMagnitude)
            )
            {
                return fallback;
            }

            float invMagnitude = 1f / Mathf.Sqrt(sqrMagnitude);

            return new Quaternion(
                rotation.x * invMagnitude,
                rotation.y * invMagnitude,
                rotation.z * invMagnitude,
                rotation.w * invMagnitude
            );
        }
    }
    
    
}