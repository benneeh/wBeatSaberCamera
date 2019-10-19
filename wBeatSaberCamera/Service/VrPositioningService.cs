using System.Numerics;
using System.Threading.Tasks;
using Valve.VR;
using wBeatSaberCamera.Annotations;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace wBeatSaberCamera.Service
{
    internal class VrPositioningService
    {
        public bool IsVrEnabled
        {
            get;
            private set;
        }

        private CVRSystem _vrSystem;

        public VrPositioningService()
        {
            EVRInitError evrInitError = default;
            Task.Run(() =>
            {
                // TODO: make vr position in combination work properly with audio emitter/listener
                _vrSystem = OpenVR.Init(ref evrInitError, EVRApplicationType.VRApplication_Other);
                IsVrEnabled = evrInitError == EVRInitError.None;
                //_isVrEnabled = true;
            });
        }

        [PublicAPI]
        public (Vector3 Position, Quaternion Rotation, Vector3 Velocity, Vector3 Omega) GetHmdPositioning()
        {
            //return (
            //    new Vector3((float)Math.Sin(sw.Elapsed.TotalSeconds * 3), 0, (float)Math.Sin(sw.Elapsed.TotalSeconds * 3)),
            //    new Quaternion(new Vector3(0, (float)Math.Sin(sw.Elapsed.TotalSeconds * 3), 0), 1),
            //    default,
            //    default);

            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                var deviceClass = _vrSystem.GetTrackedDeviceClass(i);
                if (deviceClass != ETrackedDeviceClass.Invalid)
                {
                    var deviceReading = GetDeviceReading(i);

                    if (deviceClass == ETrackedDeviceClass.HMD)
                    {
                        //Console.WriteLine("OpenVR device at " + i + ": " + deviceClass + " and rotation " + deviceReading.GetRotation());
                        return deviceReading;
                    }
                }
            }

            return default;
        }

        private (Vector3 Position, Quaternion Rotatio, Vector3 Velocity, Vector3 Omega) GetDeviceReading(uint deviceId)
        {
            // this array can be reused
            TrackedDevicePose_t[] allPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

            _vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, allPoses);

            var pose = allPoses[deviceId];
            if (pose.bPoseIsValid)
            {
                var position = RigidTransform(pose.mDeviceToAbsoluteTracking);
                var velocity = new Vector3(pose.vVelocity.v0, pose.vVelocity.v1, pose.vVelocity.v2);
                var omega = new Vector3(pose.vAngularVelocity.v0, pose.vAngularVelocity.v1, pose.vAngularVelocity.v2);

                return (position.GetPosition(), position.GetRotation(), velocity, omega);
            }

            return default;
        }

        private Matrix4x4 RigidTransform(HmdMatrix34_t pose)
        {
            var matrix = new Matrix4x4
            {
                M11 = pose.m0,
                M12 = pose.m1,
                M13 = -pose.m2,
                M14 = pose.m3,

                M21 = pose.m4,
                M22 = pose.m5,
                M23 = -pose.m6,
                M24 = pose.m7,

                M31 = -pose.m8,
                M32 = -pose.m9,
                M33 = pose.m10,
                M34 = -pose.m11
            };

            //matrix[0, 0] = pose.m0;
            //matrix[0, 1] = pose.m1;
            //matrix[0, 2] = -pose.m2;
            //matrix[0, 3] = pose.m3;

            //matrix[1, 0] = pose.m4;
            //matrix[1, 1] = pose.m5;
            //matrix[1, 2] = -pose.m6;
            //matrix[1, 3] = pose.m7;

            //matrix[2, 0] = -pose.m8;
            //matrix[2, 1] = -pose.m9;
            //matrix[2, 2] = pose.m10;
            //matrix[2, 3] = -pose.m11;
            return matrix;
        }
    }
}