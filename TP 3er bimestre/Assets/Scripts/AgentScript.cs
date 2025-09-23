using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] private Transform player;            // Asignar en inspector (o queda null y se busca por tag)
    [SerializeField] private float detectionRange = 10f;
    [SerializeField, Range(0f, 180f)] private float detectionAngle = 45f;
    [SerializeField] private LayerMask detectionMask = ~0; // por defecto: todas las capas (para evitar que no golpee nada)
    [SerializeField] private float eyeHeight = 1.6f;       // altura desde donde sale el raycast

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError($"{name} no tiene NavMeshAgent!");

        // Asegurar que el agente rote el transform (útil para usar transform.forward como referencia)
        agent.updateRotation = true;
    }

    private void Start()
    {
        // si no asignaste player en inspector, intenta buscar por tag "Player"
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
        // Detectamos siempre (si no está en modo persecución)
        if (!isChasing)
        {
            DetectPlayer();   // <- hacemos la detección primero para que funcione aunque el agente esté detenido por haber terminado patrulla
            Patrol();
        }
        else
        {
            // Persecución: seguir al jugador
            if (player != null)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }

        // Animación
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
                // Opción 2: detener al llegar al último punto
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

        // Debug visual
        Debug.DrawRay(origin, (toPlayer.normalized) * Mathf.Min(distanceToPlayer, detectionRange), Color.red);

        // Rango
        if (distanceToPlayer > detectionRange) return;

        // Ángulo
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > detectionAngle) return;

        // Raycast (usamos detectionMask para que detecte tanto player como obstáculos si configuras las capas)
        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, detectionRange, detectionMask))
        {
            // Aceptamos tanto hit al objeto player (por tag) como si el hit.transform es exactamente el transform que asignaste
            if (hit.transform == player || hit.collider.CompareTag("Player"))
            {
                Debug.Log($"{name} DETECTÓ al Player (hit: {hit.collider.name}). Pasando a perseguir.");
                isChasing = true;
                finishedPatrol = true;
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            else
            {
                // si le pegó a otra cosa, hay un obstáculo en la línea de visión
                Debug.Log($"{name} raycast bloqueado por: {hit.collider.name}");
            }
        }
        else
        {
            // no golpeó nada (posible problema con detectionMask)
            Debug.Log($"{name} Raycast no golpeó nada. Revisa detectionMask.");
        }
    }

    // Visual ayuda en el editor: radio y líneas del campo de visión
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
