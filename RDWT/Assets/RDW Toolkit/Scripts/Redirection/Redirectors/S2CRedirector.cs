using Redirection;
using UnityEngine;


public class S2CRedirector : SteerToRedirector
{
    // Testing Parameters
    private readonly bool _dontUseTempTargetInS2C = false;


    private const float S2CBearingAngleThresholdInDegree = 160;
    private const float S2CTempTargetDistance = 4;


    public override void PickRedirectionTarget()
    {
        var trackingAreaPosition = Utilities.FlattenedPos3D(RedirectionManager.trackedSpace.position);
        var userToCenter = trackingAreaPosition - RedirectionManager.currPos;

        //Compute steering target for S2C
        var bearingToCenter = Vector3.Angle(userToCenter, RedirectionManager.currDir);
        var directionToCenter = Utilities.GetSignedAngle(RedirectionManager.currDir, userToCenter);

        if (bearingToCenter >= S2CBearingAngleThresholdInDegree && !_dontUseTempTargetInS2C)
        {
            //Generate temporary target
            if (NoTmpTarget)
            {
                TMPTarget = new GameObject("S2C Temp Target");
                TMPTarget.transform.position = RedirectionManager.currPos + S2CTempTargetDistance * (Quaternion.Euler(0, directionToCenter * 90, 0) * RedirectionManager.currDir);
                TMPTarget.transform.parent = transform;
                NoTmpTarget = false;
            }

            CurrentTarget = TMPTarget.transform;
        }
        else
        {
            CurrentTarget = RedirectionManager.trackedSpace;

            if (!NoTmpTarget)
            {
                Destroy(TMPTarget);
                NoTmpTarget = true;
            }
        }
    }
}