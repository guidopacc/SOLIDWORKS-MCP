namespace SolidWorksMcp.SolidWorksWorker.Services;

internal static class SolidWorksApiConstants
{
    public const string ProgId = "SldWorks.Application";
    public const string FrontPlaneName = "Front Plane";
    public const string TopPlaneName = "Top Plane";
    public const string RightPlaneName = "Right Plane";
    public const string RefPlaneFeatureType = "RefPlane";
    public const string SketchProfileFeatureType = "ProfileFeature";
    public const string ExtrusionFeatureType = "Extrusion";

    public const int SwDocPart = 1;
    public const int SwDocAssembly = 2;
    public const int SwDocDrawing = 3;

    // This local constant mirrors swUserPreferenceStringValue_e.swDefaultTemplatePart.
    // It must stay validation-backed on a real SolidWorks 2022 machine because we do
    // not compile against the SolidWorks interop assemblies in this first slice.
    public const int SwDefaultTemplatePart = 8;

    public const int SwSaveAsCurrentVersion = 0;
    public const int SwSaveAsOptionsSilent = 1;
    public const int SwEndConditionBlind = 0;
}
