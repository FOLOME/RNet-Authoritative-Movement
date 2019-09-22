using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RasonNetwork;

public class M_Sync : NetworkedBehaviour {

    public struct transformState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public double Timestamp;
        public transformState(Vector3 pos, Quaternion rot, double time)
        {
            this.Position = pos;
            this.Rotation = rot;
            this.Timestamp = time;
        }
    }

    public transformState[] stateBuffer = new transformState[30];
    int stateCount = 0;

    [Range(0, 100)]
    public float UpdateRate = 20f;

    public float InterpolationBackTime = 1f;
    public float RotationSmooth = 15;

    Rigidbody rb;
    float updateTimer;

    // Called when the NetObject is intialized.
    void OnStart()
    {
        if (!netObject.isMine && !RasonManager.isHosting)
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; }
            transform.position = LastPosition;
            transform.rotation = LastRotation;
        }
    }
    void OnOwnerChanged()
    {
        
    }

    void bufferState(transformState state)
    {
        // shift buffer contents to accommodate new state
        for (int i = stateBuffer.Length - 1; i < stateBuffer.Length && i > 0; --i)
        {
            stateBuffer[i] = stateBuffer[i - 1];
        }
        // save state to slot 0
        stateBuffer[0] = state;
        // increment state count
        stateCount = Mathf.Min(stateCount + 1, stateBuffer.Length);
    }

    Vector3 LastPosition = new Vector3(0, -99999, 0);
    Quaternion LastRotation = new Quaternion(0, 0, 0, 0);

    void Update()
    {
        if (RasonManager.isHosting)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= (1 / UpdateRate))
            {
                updateTimer = 0f;

                netObject.CallRpc(251, TargetPlayer.OthersSaved, false, transform.position, transform.rotation, RasonManager.time);
                bufferState(new transformState(transform.position, transform.rotation, RasonManager.time));
            }
            return;
        }

        if (netObject.isMine) return;

        if (stateCount == 0) return; // no states to interpolate

        double currentTime = RasonManager.time;
        double interpolationTime = currentTime - InterpolationBackTime;

        // the latest packet is newer than interpolation time - we have enough packets to interpolate
        if (stateBuffer[0].Timestamp > interpolationTime)
        {
            for (int i = 0; i < stateCount; i++)
            {
                // find the closest state that matches network time, or use oldest state
                if (stateBuffer[i].Timestamp <= interpolationTime || i == stateCount - 1)
                {
                    // the state closest to network time
                    transformState lhs = stateBuffer[i];
                    // the state one slot newer
                    transformState rhs = stateBuffer[Mathf.Max(i - 1, 0)];
                    // use time between lhs and rhs to interpolate
                    double length = rhs.Timestamp - lhs.Timestamp;

                    float t = 0f;
                    if (length > 0.0001)
                    {
                        t = (float)((interpolationTime - lhs.Timestamp) /
                        length);
                    }
                    transform.position = Vector3.Lerp(lhs.Position, rhs.Position, t);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rhs.Rotation, Time.deltaTime * RotationSmooth);
                    break;
                }
            }
        }
    }

    [RRPC(251)]
    void NetGetData(Vector3 pos, Quaternion rot, double timestamp)
    {
        LastPosition = pos;
        LastRotation = rot;

        bufferState(new transformState(LastPosition, LastRotation, timestamp));
    }
}
