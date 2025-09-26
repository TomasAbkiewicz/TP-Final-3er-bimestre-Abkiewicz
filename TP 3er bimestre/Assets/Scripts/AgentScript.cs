using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AgentScript : MonoBehaviour
{
    private NavMeshAgent agent;


    [SerializeField] private List<Transform> targets = new List<Transform>();
    private int currentTargetIndex = 0;
    [SerializeField] private float reachThreshold = 0.5f;
    private bool isChasing = false;
    private bool finishedPatrol = false;

    [Header("Animación")]
    [SerializeField] private Animator anim;

    [Header("Detección del jugador")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField, Range(0f, 180f)] private float detectionAngle = 45f;
    [SerializeField] private LayerMask detectionMask = ~0;
    [SerializeField] private float eyeHeight = 1.6f;


    [SerializeField] private Text messageUI;

    // --- NUEVO: control de persecución ---
    private float chaseTimer = 0f;
    [SerializeField] private float maxChaseTime = 2f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError($"{name} no tiene NavMeshAgent!");
        agent.updateRotation = true;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (targets != null && targets.Count > 0)
        {
            finishedPatrol = false;
            agent.isStopped = false;
            agent.SetDestination(targets[currentTargetIndex].position);
        }
        else
        {
            finishedPatrol = true;
            agent.isStopped = true;
        }

        if (messageUI != null) messageUI.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isChasing)
        {
            DetectPlayer();
            Patrol();
        }
        else
        {
            if (player != null)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);

                chaseTimer += Time.deltaTime;
                if (chaseTimer >= maxChaseTime)
                {
                    Debug.Log($"{name} no atrapó al Player en {maxChaseTime} seg → vuelve a patrullar.");
                    RestartPatrol();
                }
            }
        }

        if (anim != null)
            anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    private void Patrol()
    {
        if (finishedPatrol) return;
        if (targets == null || targets.Count == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= reachThreshold)
        {
            currentTargetIndex++;
            if (currentTargetIndex >= targets.Count)
            {
                finishedPatrol = true;
                agent.isStopped = true;
                Debug.Log($"{name} terminó patrulla.");
                return;
            }
            agent.SetDestination(targets[currentTargetIndex].position);
        }
    }

    private void DetectPlayer()
    {
        if (player == null) return;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = player.position - origin;
        float distanceToPlayer = toPlayer.magnitude;

        Debug.DrawRay(origin, (toPlayer.normalized) * Mathf.Min(distanceToPlayer, detectionRange), Color.red);

        if (distanceToPlayer > detectionRange) return;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > detectionAngle) return;

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, detectionRange, detectionMask))
        {
            if (hit.transform == player || hit.collider.CompareTag("Player"))
            {
                Debug.Log($"{name} DETECTÓ al Player (hit: {hit.collider.name}). Pasando a perseguir.");
                isChasing = true;
                finishedPatrol = true;
                agent.isStopped = false;
                agent.SetDestination(player.position);

                chaseTimer = 0f;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"{name} atrapó al jugador!");

            if (messageUI != null)
            {
                messageUI.text = "¡Has sido atrapado!";
                messageUI.gameObject.SetActive(true);
            }

            agent.isStopped = true;
            Invoke(nameof(GoToGameOver), 2f);
        }
    }

    private void GoToGameOver()
    {
        SceneManager.LoadScene(1);
    }

    private void RestartPatrol()
    {
        isChasing = false;
        finishedPatrol = false;
        chaseTimer = 0f;

        if (targets.Count == 0) return;

        // waypoint aleatorio
        currentTargetIndex = Random.Range(0, targets.Count);
        agent.isStopped = false;
        agent.SetDestination(targets[currentTargetIndex].position);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, detectionRange);

        Vector3 forward = transform.forward;
        Quaternion leftRot = Quaternion.Euler(0, -detectionAngle, 0);
        Quaternion rightRot = Quaternion.Euler(0, detectionAngle, 0);
        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, leftDir * detectionRange);
        Gizmos.DrawRay(origin, rightDir * detectionRange);
    }
}
