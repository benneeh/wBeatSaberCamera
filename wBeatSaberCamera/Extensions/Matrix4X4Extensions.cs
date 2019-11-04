// ReSharper disable once CheckNamespace
namespace System.Numerics
{
    public static class Matrix4X4Extensions
    {
        public static Microsoft.Xna.Framework.Vector3 GetPosition(this Matrix4x4 matrix)
        {
            float x = matrix.M14;
            float y = matrix.M24;
            float z = matrix.M34;

            return new Microsoft.Xna.Framework.Vector3(x, y, z);
        }

        public static Microsoft.Xna.Framework.Quaternion GetRotation(this Matrix4x4 matrix)
        {
            var q = new Microsoft.Xna.Framework.Quaternion();
            q.X = (float)Math.Sqrt(Math.Max(0, 1f + matrix.M11 - matrix.M22 - matrix.M33)) / 2f;
            q.Y = (float)Math.Sqrt(Math.Max(0, 1f - matrix.M11 + matrix.M22 - matrix.M33)) / 2f;
            q.W = (float)Math.Sqrt(Math.Max(0, 1f + matrix.M11 + matrix.M22 + matrix.M33)) / 2f;
            q.Z = (float)Math.Sqrt(Math.Max(0, 1f - matrix.M11 - matrix.M22 + matrix.M33)) / 2f;
            q.X = CopySign(q.X, matrix.M32 - matrix.M23);
            q.Y = CopySign(q.Y, matrix.M13 - matrix.M31);
            q.Z = CopySign(q.Z, matrix.M21 - matrix.M12);
            return q;
        }

        private static float CopySign(float length, float signTester)
        {
            return Math.Sign(signTester) == 1 ? Math.Abs(length) : -Math.Abs(length);
        }
    }
}