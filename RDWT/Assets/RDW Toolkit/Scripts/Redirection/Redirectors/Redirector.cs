using Redirection;
using UnityEngine;


public abstract class Redirector : MonoBehaviour
{
    [HideInInspector]
    public RedirectionManager RedirectionManager;


    /// <summary>
    ///     Applies redirection based on the algorithm.
    /// </summary>
    public abstract void ApplyRedirection();


    /// <summary>
    ///     Applies rotation to Redirected User. The neat thing about calling it this way is that we can keep track of gains
    ///     applied.
    /// </summary>
    /// <param name="rotationInDegrees"></param>
    protected void InjectRotation(float rotationInDegrees)
    {
        if (rotationInDegrees != 0)
        {
            transform.RotateAround(Utilities.FlattenedPos3D(RedirectionManager.headTransform.position), Vector3.up, rotationInDegrees);
            GetComponentInChildren<KeyboardController>().SetLastRotation(rotationInDegrees);
            RedirectionManager.statisticsLogger.Event_Rotation_Gain(rotationInDegrees / RedirectionManager.deltaDir, rotationInDegrees);
        }
    }


    /// <summary>
    ///     Applies curvature to Redirected User. The neat thing about calling it this way is that we can keep track of gains
    ///     applied.
    /// </summary>
    /// <param name="rotationInDegrees"></param>
    protected void InjectCurvature(float rotationInDegrees)
    {
        if (rotationInDegrees != 0)
        {
            transform.RotateAround(Utilities.FlattenedPos3D(RedirectionManager.headTransform.position), Vector3.up, rotationInDegrees);
            GetComponentInChildren<KeyboardController>().SetLastCurvature(rotationInDegrees);
            RedirectionManager.statisticsLogger.Event_Curvature_Gain(rotationInDegrees / RedirectionManager.deltaPos.magnitude, rotationInDegrees);
        }
    }


    /// <summary>
    ///     Applies rotation to Redirected User. The neat thing about calling it this way is that we can keep track of gains
    ///     applied.
    /// </summary>
    /// <param name="translation"></param>
    protected void InjectTranslation(Vector3 translation)
    {
        if (translation.magnitude > 0)
        {
            transform.Translate(translation, Space.World);
            GetComponentInChildren<KeyboardController>().SetLastTranslation(translation);
            RedirectionManager.statisticsLogger.Event_Translation_Gain(Mathf.Sign(Vector3.Dot(translation, RedirectionManager.deltaPos)) * translation.magnitude / RedirectionManager.deltaPos.magnitude, Utilities.FlattenedPos2D(translation));

            if (double.IsNaN(Mathf.Sign(Vector3.Dot(translation, RedirectionManager.deltaPos)) * translation.magnitude / RedirectionManager.deltaPos.magnitude))
            {
                print("wtf");
            }
        }
    }
}