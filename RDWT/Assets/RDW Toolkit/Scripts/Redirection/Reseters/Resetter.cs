using Redirection;
using UnityEngine;


public abstract class Resetter : MonoBehaviour
{
    [HideInInspector]
    public RedirectionManager redirectionManager;

    private float _maxX, _maxZ;


    /// <summary>
    ///     Function called when reset trigger is signaled, to see if resetter believes resetting is necessary.
    /// </summary>
    /// <returns></returns>
    public abstract bool IsResetRequired();


    public abstract void InitializeReset();


    public abstract void ApplyResetting();


    public abstract void FinalizeReset();


    public abstract void SimulatedWalkerUpdate();


    public void InjectRotation(float rotationInDegrees)
    {
        transform.RotateAround(Utilities.FlattenedPos3D(redirectionManager.headTransform.position), Vector3.up, rotationInDegrees);
        GetComponentInChildren<KeyboardController>().SetLastRotation(rotationInDegrees);
        redirectionManager.statisticsLogger.Event_Rotation_Gain_Reorientation(rotationInDegrees / redirectionManager.deltaDir, rotationInDegrees);
    }


    public void Initialize()
    {
        _maxX = 0.5f * redirectionManager.trackedSpace.localScale.x - redirectionManager.resetTrigger.ResetTriggerBuffer; // redirectionManager.resetTrigger.xLength);// + USER_CAPSULE_COLLIDER_DIAMETER);
        _maxZ = 0.5f * redirectionManager.trackedSpace.localScale.z - redirectionManager.resetTrigger.ResetTriggerBuffer;
        //print("PRACTICAL MAX X: " + maxX);
    }


    public bool IsUserOutOfBounds()
    {
        return Mathf.Abs(redirectionManager.currPosReal.x) >= _maxX || Mathf.Abs(redirectionManager.currPosReal.z) >= _maxZ;
    }


    private Boundary GetNearestBoundary()
    {
        var position = redirectionManager.currPosReal;

        if (position.x >= 0 && Mathf.Abs(_maxX - position.x) <= Mathf.Min(Mathf.Abs(_maxZ - position.z), Mathf.Abs(-_maxZ - position.z))) // for a very wide rectangle, you can find that the first condition is actually necessary
        {
            return Boundary.Right;
        }

        if (position.x <= 0 && Mathf.Abs(-_maxX - position.x) <= Mathf.Min(Mathf.Abs(_maxZ - position.z), Mathf.Abs(-_maxZ - position.z)))
        {
            return Boundary.Left;
        }

        if (position.z >= 0 && Mathf.Abs(_maxZ - position.z) <= Mathf.Min(Mathf.Abs(_maxX - position.x), Mathf.Abs(-_maxX - position.x)))
        {
            return Boundary.Top;
        }

        return Boundary.Bottom;
    }


    private Vector3 GetAwayFromNearestBoundaryDirection()
    {
        var nearestBoundary = GetNearestBoundary();

        switch (nearestBoundary)
        {
            case Boundary.Top:
                return -Vector3.forward;
            case Boundary.Bottom:
                return Vector3.forward;
            case Boundary.Right:
                return -Vector3.right;
            case Boundary.Left:
                return Vector3.right;
        }

        return Vector3.zero;
    }


    private float GetUserAngleWithNearestBoundary() // Away from Wall is considered Zero
    {
        return Utilities.GetSignedAngle(redirectionManager.currDirReal, GetAwayFromNearestBoundaryDirection());
    }


    protected bool IsUserFacingAwayFromWall()
    {
        return Mathf.Abs(GetUserAngleWithNearestBoundary()) < 90;
    }


    public float GetTrackingAreaHalfDiameter()
    {
        return Mathf.Sqrt(_maxX * _maxX + _maxZ * _maxZ);
    }


    public float GetDistanceToCenter()
    {
        return redirectionManager.currPosReal.magnitude;
    }


    public float GetDistanceToNearestBoundary()
    {
        var position = redirectionManager.currPosReal;
        var nearestBoundary = GetNearestBoundary();

        switch (nearestBoundary)
        {
            case Boundary.Top:
                return Mathf.Abs(_maxZ - position.z);
            case Boundary.Bottom:
                return Mathf.Abs(-_maxZ - position.z);
            case Boundary.Right:
                return Mathf.Abs(_maxX - position.x);
            case Boundary.Left:
                return Mathf.Abs(-_maxX - position.x);
        }

        return 0;
    }


    public float GetMaxWalkableDistanceBeforeReset()
    {
        var position = redirectionManager.currPosReal;
        var direction = redirectionManager.currDirReal;
        var tMaxX = direction.x != 0 ? Mathf.Max((_maxX - position.x) / direction.x, (-_maxX - position.x) / direction.x) : float.MaxValue;
        var tMaxZ = direction.z != 0 ? Mathf.Max((_maxZ - position.z) / direction.z, (-_maxZ - position.z) / direction.z) : float.MaxValue;

        //print("MaxX: " + maxX);
        //print("MaxZ: " + maxZ);
        return Mathf.Min(tMaxX, tMaxZ);
    }


    private enum Boundary
    {
        Top,
        Bottom,
        Right,
        Left
    }
}