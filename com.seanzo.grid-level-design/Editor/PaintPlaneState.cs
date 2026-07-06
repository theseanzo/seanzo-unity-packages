using UnityEditor;

namespace Seanzo.LevelDesign.Editor
{
    public static class PaintPlaneState
    {
        private const string OrientationKey = "Seanzo.LevelDesign.PaintPlane.Orientation";
        private const string IndexKeyPrefix = "Seanzo.LevelDesign.PaintPlane.Index.";
        private const string FarSideKey = "Seanzo.LevelDesign.PaintPlane.FarSide";
        private const string GhostPlanesKey = "Seanzo.LevelDesign.PaintPlane.Ghosts";

        public static PaintPlane ActivePlane
        {
            get
            {
                var orientation = (PlaneOrientation)SessionState.GetInt(OrientationKey, (int)PlaneOrientation.XZ);
                return PlaneFor(orientation);
            }
            set
            {
                SessionState.SetInt(OrientationKey, (int)value.orientation);
                SessionState.SetInt(IndexKey(value.orientation), value.index);
            }
        }

        // Each orientation remembers its own slice index across switches.
        public static PaintPlane PlaneFor(PlaneOrientation orientation)
        {
            return new PaintPlane(orientation, SessionState.GetInt(IndexKey(orientation), 0));
        }

        // Paint side for vertical planes: near targets the face at the drawn boundary
        // on the higher-index cell; far targets the opposite face on the lower-index cell.
        public static bool FarSide
        {
            get => SessionState.GetBool(FarSideKey, false);
            set => SessionState.SetBool(FarSideKey, value);
        }

        public static bool GhostPlanes
        {
            get => SessionState.GetBool(GhostPlanesKey, false);
            set => SessionState.SetBool(GhostPlanesKey, value);
        }

        public static void SetOrientation(PlaneOrientation orientation)
        {
            SessionState.SetInt(OrientationKey, (int)orientation);
        }

        public static void StepIndex(int delta)
        {
            var plane = ActivePlane;
            plane.index += delta;
            ActivePlane = plane;
        }

        private static string IndexKey(PlaneOrientation orientation)
        {
            return IndexKeyPrefix + (int)orientation;
        }
    }
}
