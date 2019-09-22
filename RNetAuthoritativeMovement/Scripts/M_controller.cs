using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RasonNetwork;

public class M_controller : NetworkedBehaviour
{

    private struct move
    {
        public float HorizontalAxis;
        public float VerticalAxis;
        public double Timestamp;
        public int senderID;
        public Vector3 pos;
        public move(float horiz, float vert, double timestamp, int sender, Vector3 Pos)
        {
            this.HorizontalAxis = horiz;
            this.VerticalAxis = vert;
            this.Timestamp = timestamp;
            this.senderID = sender;
            this.pos = Pos;
        }
    }

    public float MoveSpeed = 5f;
    public float MaxDistanceBetweenClientAndServerSide = 0.1f;

    private float horizAxis = 0f;
    private float vertAxis = 0f;

    // a history of move states sent from client to server
    RList<move> moveHistory = new RList<move>();
    Queue<move> moves = new Queue<move>();

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (netObject.isMine)
        {
            horizAxis = Input.GetAxis("Horizontal");
            vertAxis = Input.GetAxis("Vertical");
        }
    }
    void FixedUpdate()
    {
        if (netObject.isMine)
        {
            // get current move state
            move moveState = new move(horizAxis, vertAxis, RasonManager.time, RasonManager.Id, transform.position);

            // buffer move state
            moveHistory.Insert(0, moveState);

            // cap history at 200
            if (moveHistory.Count > 200)
            {
                moveHistory.RemoveAt(moveHistory.Count - 1);
            }

            // simulate
            Simulate();

            // send state to host
            netObject.CallRpc(5, TargetPlayer.Host, false,
                moveState.HorizontalAxis, moveState.VerticalAxis,
                transform.position, RasonManager.Id);
        }
        else if (RasonManager.isHosting)
        {
            if (moves.Count != 0)
            {
                ValidInput(moves.Dequeue());
            }
        }
    }
    [RRPC(5)]
    void ProcessInput(float ha, float va, Vector3 position, int SenderID)
    {
        if (netObject.isMine)
            return;
        if (!RasonManager.isHosting)
            return;
        moves.Enqueue(new move(ha, va, RasonManager.time, SenderID, position));

    }
    void ValidInput(move m)
    {
        // execute input
        horizAxis = m.HorizontalAxis;
        vertAxis = m.VerticalAxis;

        Simulate();

        // compare results
        if (Vector3.Distance(transform.position, m.pos) > MaxDistanceBetweenClientAndServerSide)
        {
            // error is too big, tell client to rewind and replay

            netObject.CallRpc("CorrectState", m.senderID, true, transform.position, RasonManager.time);
        }
    }

    [RRPC]
    void CorrectState(Vector3 correctPosition, double timestamp)
    {
        // find past state based on timestamp
        int pastState = 0;
        for (int i = 0; i < moveHistory.Count; i++)
        {
            if (moveHistory[i].Timestamp <= timestamp)
            {
                pastState = i;
                break;
            }
        }

        // rewind
        transform.position = correctPosition;

        // replay
        for (int i = pastState; i >= 0; i--)
        {
            horizAxis = moveHistory[i].HorizontalAxis;
            vertAxis = moveHistory[i].VerticalAxis;
            Simulate();
        }
        // clear
        moveHistory.Clear();
    }
    public void Simulate()
    {
        rb.MovePosition(rb.position + transform.right * horizAxis * MoveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + transform.forward * vertAxis * MoveSpeed * Time.fixedDeltaTime);
        //transform.Translate(new Vector3(horizAxis, 0, vertAxis) * MoveSpeed * Time.fixedDeltaTime);
    }
}
