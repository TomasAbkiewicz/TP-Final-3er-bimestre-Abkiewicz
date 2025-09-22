using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AgentScript : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Patrullaje")]
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

    [Header("Derrota del jugador")]
    [SerializeField] private string loseSceneName = "GameOver"; // Nombre de la escena de derrota
    [SerializeField] private Text loseMessageUI;                // Texto UI en canvas
    [SerializeField] private string loseMessage = "¡Has sido atrapado!";
    [SerializeField] private float loseDelay = 2f;              // Tiempo antes de cambiar de escena

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

        Debug.DrawRay(origin, toPlayer.normalized * Mathf.Min(distanceToPlayer, detectionRange), Color.red);

        if (distanceToPlayer > detectionRange) return;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > detectionAngle) return;

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, detectionRange, detectionMask))
        {
            if (hit.transform == player || hit.collider.CompareTag("Player"))
            {
                isChasing = true;
                finishedPatrol = true;
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerLose();
        }
    }

    private void PlayerLose()
    {
        Debug.Log("Jugador atrapado -> GAME OVER");

        // Mostrar mensaje en UI si está asignado
        if (loseMessageUI != null)
        {
            loseMessageUI.gameObject.SetActive(true);
            loseMessageUI.text = loseMessage;
        }

        // Cambiar de escena después del delay
        Invoke(nameof(LoadLoseScene), loseDelay);
    }

    private void LoadLoseScene()
    {
        SceneManager.LoadScene(loseSceneName);
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
