using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// A hummingbird Machine Learning Agent
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak")]
    public Transform beakTip;

    [Tooltip("The agent's camera")]
    public Camera agentCamera;

    [Tooltip("Whether this is training mode or gameplay mode")]
    public bool trainingMode;

    // The rigidbody of the agent
    new private Rigidbody rigidbody;

    // The flower area that the agent is in
    private FlowerArea flowerArea;

    // The nearest flower to the agent
    public GameObject nearestFlower;

    // Allows for smoother pitch changes
    private float smoothPitchChange = 0f;

    // Allows for smoother yaw changes
    private float smoothYawChange = 0f;

    // Maximum angle that the bird can pitch up or down
    private const float MaxPitchAngle = 80f;

    // Maximum distance from the beak tip to accept nectar collision
    private const float BeakTipRadius = 0.008f;

    // Whether the agent is frozen (intentionally not flying)
    private bool frozen = false;

    /// The amount of nectar the agent has obtained this episode
    public float NectarObtained { get; private set; }

    public float targetTime;

    private float time;

    private GameObject lastPoint;

    private float lastPosition;

    private float reward;

    private bool batHit;

    /// Initialize the agent
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        // If not training mode, no max step, play forever
        if (!trainingMode) MaxStep = 0;
    }

    /// Reset the agent when an episode begins
    public override void OnEpisodeBegin()
    {
        // Reset nectar obtained
        NectarObtained = 0f;

        // Zero out velocities so that movement stops before a new episode begins
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        //Pick a random target from the array
        FindTarget();

        // Move the agent to a new random position
        MoveToSafeRandomPosition();
    }

    /// Called when and action is received from either the player input or the neural network
    /// 
    /// vectorAction[i] represents:
    /// Index 0: move vector x (+1 = right, -1 = left)
    /// Index 1: move vector y (+1 = up, -1 = down)
    /// Index 2: move vector z (+1 = forward, -1 = backward)
    /// Index 3: pitch angle (+1 = pitch up, -1 = pitch down)
    /// Index 4: yaw angle (+1 = turn right, -1 = turn left)
    public override void OnActionReceived(float[] vectorAction)
    {
        // Don't take actions if frozen
        if (frozen) return;

        // Calculate movement vector
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);

        // Add force in the direction of the move vector
        rigidbody.AddForce(move * moveForce);

        // Get the current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles;

        // Calculate pitch and yaw rotation
        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4];

        // Calculate smooth rotation changes
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        // Calculate new pitch and yaw based on smoothed values
        // Clamp  pitch to avoid flipping upside down
        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        // Apply the new rotation
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// Collect vector observations from the environment
    public override void CollectObservations(VectorSensor sensor)
    {
        // If nearestFlower is null, observe an empty array and return early
        /*if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }*/

        // Observe the agent's local rotation (4 observations)
        //sensor.AddObservation(transform.localRotation.normalized);

        // Get a vector from the beak tip to the nearest flower
        Vector3 toFlower = nearestFlower.transform.position - beakTip.position;

        // Observe a normalized vector pointing to the nearest flower (3 observations)
        sensor.AddObservation(toFlower.normalized);

        // Observe the relative distance from the beak tip to the flower (1 observation)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);

        Vector3 toPlayer = GameManager.instance.player.transform.position - transform.position;

        sensor.AddObservation(toPlayer.normalized);

        sensor.AddObservation(toPlayer.magnitude / FlowerArea.AreaDiameter);
        // 10 total observations

    }

    /// When Behavior Type is set to "Heuristic Only" on the agent's Behavior Parameters,
    /// this function will be called. Its return values will be fed into
    public override void Heuristic(float[] actionsOut)
    {
        // Create placeholders for all movement/turning
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        // Convert keyboard inputs to movement and turning
        // All values should be between -1 and +1

        // Forward/backward
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        // Left/right
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        // Up/down
        if (Input.GetKey(KeyCode.E)) up = transform.up;
        else if (Input.GetKey(KeyCode.C)) up = -transform.up;

        // Pitch up/down
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        // Turn left/right
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        // Combine the movement vectors and normalize
        Vector3 combined = (forward + left + up).normalized;

        // Add the 3 movement values, pitch, and yaw to the actionsOut array
        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;
        actionsOut[3] = pitch;
        actionsOut[4] = yaw;
    }

    /// Prevent the agent from moving and taking actions
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        if (!trainingMode)
        {
            frozen = true;
            rigidbody.Sleep();
        }
    }

    /// Resume agent movement and actions
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        if(!trainingMode)
        {
            frozen = false;
            rigidbody.WakeUp();
        }
    }

    /// Move the agent to a safe random position (i.e. does not collide with anything)
    /// If in front of flower, also point the beak at the flower
    private void MoveToSafeRandomPosition()
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100; // Prevent an infinite loop
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        // Loop until a safe position is found or we run out of attempts
        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;

            // Pick a random height from the ground
            float height = UnityEngine.Random.Range(1.2f, 2.5f);

            // Pick a random radius from the center of the area
            float radius = UnityEngine.Random.Range(2f, 7f);

            // Pick a random direction rotated around the y axis
            Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);

            // Combine height, radius, and direction to pick a potential position
            potentialPosition = flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

            // Choose and set random starting pitch and yaw
            float pitch = UnityEngine.Random.Range(-60f, 60f);
            float yaw = UnityEngine.Random.Range(-180f, 180f);
            potentialRotation = Quaternion.Euler(pitch, yaw, 0f);

            // Check to see if the agent will collide with anything
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            // Safe position has been found if no colliders are overlapped
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

        // Set the position and rotation
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }

    /// Called when the agent's collider enters a trigger collider
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);

        if (other.tag == "BatRadius")
        {
            batHit = true;
        }

    }

    /// Called when the agent's collider stays in a trigger collider
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.tag == "BatRadius")
        {
            batHit = false;
        }
    }

    /// Handles when the agent's collider enters or stays in a trigger collider
    private void TriggerEnterOrStay(Collider collider)
    {
        // Check if agent is colliding with nectar
        if (collider.CompareTag("nectar"))
        {
            if (collider.gameObject == nearestFlower)
            {
                AddReward(1f);
                reward += 1f;

                time += Time.deltaTime;
                if (time >= targetTime)
                {
                    time = 0;
                    FindTarget();
                    AddReward(10f);

                    reward += 10f;

                    GameManager.instance.enemyScore++;
                }
            }
        }

        if(collider.CompareTag("Player"))
        {
            if (trainingMode)
            {
                AddReward(-5f);
                reward += -5f;
                EndEpisode();
            }
            else
            {
                GameManager.instance.mosquitoList.Remove(gameObject);
                Destroy(gameObject);
            }
        }
    }

    /// Called when the agent collides with something solid
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            // Collided with the area boundary, give a negative reward
            AddReward(-.5f);
        }
    }

    private void FindTarget()
    {
        lastPoint = nearestFlower;
        while (nearestFlower == lastPoint)
        {
            FlowerArea playArea = GetComponentInParent<FlowerArea>();
            nearestFlower = playArea.targetPoints[Random.Range(0, playArea.targetPoints.Length)];
        }
    }
    /// <summary>
    /// Called every .02 seconds
    /// </summary>
    private void Update()
    {
        Quaternion rot = Quaternion.LookRotation(rigidbody.velocity);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime);

        if (Vector3.Distance(transform.position, nearestFlower.transform.position) < lastPosition)
        {
            AddReward(0.02f);
            reward += 0.02f;
        }
        else
        {
            //AddReward(-0.001f);
            //reward += -0.001f;
        }

        if(batHit == true)
        {
            AddReward(-0.2f);
            reward += -0.2f;
        }
        else
        {
            AddReward(0.01f);
            reward += 0.01f;
        }

        lastPosition = Vector3.Distance(transform.position, nearestFlower.transform.position);
    }
}
