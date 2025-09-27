using System;
using System.Collections.Generic;
using UnityEngine;

public static class TrainUtilities
{
    /// <summary>
    /// Since the ArticulationBody's xDrive, yDrive, zDrive uses deg, deg/s as target and target velocity,
    /// target and target velocity should be defined in deg, deg/s.
    /// </summary>
    /// <param name="targets"> deg.</param>
    /// <param name="targetVelocities"> deg/s.</param>
    /// <param name="_totalJointDofCount"> Total joint dof count.</param>
    /// <param name="jointArticulationBodies"> Joint Articulation Bodies.</param>
    /// <param name="jointArticulationBodyFreeDofs"> Joint Articulation Body Free DoFs.</param>
    /// <param name="additive"> If true, then the target will be added to the current target.</param>
    /// <exception cref="Exception">Articulation Drives' target length is not matched with the total joint dof count.</exception>
    public static void SetArticulationDrives(IList<float> targets, IList<float> targetVelocities,
        int _totalJointDofCount, ArticulationBody[] jointArticulationBodies,
        (bool, bool, bool)[] jointArticulationBodyFreeDofs,
        float _dt, bool additive = false)
    {
        if (targets.Count != _totalJointDofCount)
            throw new Exception(
                "Articulation Drives' target length is not matched with the total joint dof count.");
        if (targetVelocities.Count != _totalJointDofCount)
            throw new Exception(
                "Articulation Drives' target velocity length is not matched with the total joint dof count.");

        int ddx = 0;
        for (int jdx = 0; jdx < jointArticulationBodies.Length; jdx++)
        {
            if (jointArticulationBodyFreeDofs[jdx].Item1)
            {
                var xDrive = jointArticulationBodies[jdx].xDrive;
                xDrive.stiffness = 1 / _dt;
                xDrive.damping = 0.1f;
                float target = additive ? xDrive.target + targets[ddx] : targets[ddx];
                xDrive.target = Mathf.Clamp(target, xDrive.lowerLimit, xDrive.upperLimit);
                xDrive.targetVelocity = targetVelocities[ddx];
                jointArticulationBodies[jdx].xDrive = xDrive;
                ddx += 1;
            }
            if (jointArticulationBodyFreeDofs[jdx].Item2)
            {
                var yDrive = jointArticulationBodies[jdx].yDrive;
                yDrive.stiffness = 1 / _dt;
                yDrive.damping = 0.1f;
                float target = additive ? yDrive.target + targets[ddx] : targets[ddx];
                yDrive.target = Mathf.Clamp(target, yDrive.lowerLimit, yDrive.upperLimit);
                yDrive.targetVelocity = targetVelocities[ddx];
                jointArticulationBodies[jdx].yDrive = yDrive;
                ddx += 1;
            }
            if (jointArticulationBodyFreeDofs[jdx].Item3)
            {
                var zDrive = jointArticulationBodies[jdx].zDrive;
                zDrive.stiffness = 1 / _dt;
                zDrive.damping = 0.1f;
                float target = additive ? zDrive.target + targets[ddx] : targets[ddx];
                zDrive.target = Mathf.Clamp(target, zDrive.lowerLimit, zDrive.upperLimit);
                zDrive.targetVelocity = targetVelocities[ddx];
                jointArticulationBodies[jdx].zDrive = zDrive;
                ddx += 1;
            }
        }
    }
}