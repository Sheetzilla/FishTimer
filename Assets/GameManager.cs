using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject pikku;
    public float speed = 100f;
    public GameObject leftWall;
    public GameObject rightWall;
    public int spawnAmount = 10;
    public int spawned = 0;
    public List<GameObject> cubes = new List<GameObject>();
    [SerializeField]
    private AudioClip Blop2;
    public float blopVolume = 1f;

    // World Y below which a dropped cube is destroyed. Set this in the Inspector
    // to a height comfortably below the bottom of the visible play area.
    public float despawnY = -10f;

    private timer timerScript;
    private ScriptKuutio cube;
    private Coroutine dropTimerRoutine;

    // Cubes already mid-removal, so dropCubes never flags the same one twice.
    private HashSet<GameObject> removing = new HashSet<GameObject>();

    // Cube count captured when the timer starts, so the pile drains linearly
    // against this fixed baseline instead of compounding off the shrinking count.
    private int startingCubeCount = 0;

    // Wall-clock time (epoch seconds) of the last dropCubes() call.
    // Used so the drop loop stays synced to real time even when the tab sleeps.
    private double lastDropRealTime = 0;

    void Start()
    {
        timerScript = GetComponent<timer>();
        cube = GetComponent<ScriptKuutio>();

        Vector3 left = Camera.main.ScreenToWorldPoint(
            new Vector3(-40f, Screen.height / 2, 10));
        Vector3 right = Camera.main.ScreenToWorldPoint(
            new Vector3(Screen.width + 40f, Screen.height / 2, 10));
        leftWall.transform.position = left;
        rightWall.transform.position = right;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // If the main cube is clicked after the timer ran out and the timer is beeping,
                // stop the beeping and trigger the click animation — don't spawn minis.
                if (hit.collider.gameObject.CompareTag("Kuutio") &&
                    timerScript != null &&
                    timerScript.timeLimit <= 0 &&
                    timerScript.IsBeeping)
                {
                    timerScript.StopBeeping();
                    hit.collider.gameObject.GetComponent<Animator>()?.SetTrigger("click");
                    return;
                }

                if (hit.collider.gameObject.CompareTag("Kuutio") &&
                    spawned < 300 &&
                    timerScript.timeLimit > 0 &&
                    !cube.isSpinning)
                {
                    // Play click sound
                    if (audioManager.instance != null && Blop2 != null)
                        audioManager.instance.PlayAudio(Blop2, hit.collider.gameObject.transform, 0.5f);

                    StartCoroutine(spawnMinis(hit));
                    hit.collider.gameObject.GetComponent<Animator>().SetTrigger("click");
                }
            }
        }
    }

    IEnumerator spawnMinis(RaycastHit hit)
    {
        for (int i = 0; i < spawnAmount; i++)
        {
            yield return new WaitForSeconds(0.1f);

            // Scatter the spawn point so 300 bodies aren't born inside each other
            // (overlapping spawns are the single biggest cause of the jitter).
            Vector3 jitter = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.3f, 0.3f),
                0f);

            GameObject spawnattu = Instantiate(
                pikku,
                hit.collider.gameObject.transform.position + jitter,
                Quaternion.identity);

            Rigidbody rb = spawnattu.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.sleepThreshold = 0.05f;
                rb.maxDepenetrationVelocity = 1f;
#if UNITY_2022_3_OR_NEWER
                rb.linearDamping = 0.1f;
                rb.angularDamping = 0.5f;
#else
                rb.drag = 0.1f;
                rb.angularDrag = 0.5f;
#endif
                rb.AddForce(new Vector3(
                    Random.Range(-1f, 2f) * speed,
                    Random.Range(-1f, 2f) * speed,
                    0));
            }
            cubes.Add(spawnattu);
        }
        spawned = cubes.Count;

        if (spawned >= 240)
        {
            startingCubeCount = cubes.Count;
            timerScript.StartTimer();

            // Fisher-Yates shuffle so cubes drop in random order.
            for (int i = 0; i < cubes.Count; i++)
            {
                int j = Random.Range(i, cubes.Count);
                GameObject temp = cubes[i];
                cubes[i] = cubes[j];
                cubes[j] = temp;
            }
        }
    }

    public void dropCubes()
    {
        if (timerScript.timerIsRunning == false)
            return;
        if (timerScript.timeLimit <= 0)
            return;

        float percentRemaining = timerScript.timeRemaining / timerScript.timeLimit;
        // Target keep-count is a fraction of the ORIGINAL pile, not the current
        // (shrinking) one — otherwise the percentage compounds each second and the
        // pile collapses far too fast.
        int cubesToKeep = Mathf.RoundToInt(startingCubeCount * percentRemaining);

        for (int i = cubes.Count - 1; i >= 0; i--)
        {
            if (i >= cubesToKeep && !removing.Contains(cubes[i]))
            {
                StartCoroutine(RemoveCubeAfterDelay(cubes[i]));
            }
        }
    }

    IEnumerator RemoveCubeAfterDelay(GameObject cube)
    {
        removing.Add(cube);

        // Disable the collider so this cube falls through everything below it.
        Collider col = cube.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Wake the cube and give it a small downward nudge.
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.WakeUp();
            rb.AddForce(Vector3.down * 2f, ForceMode.VelocityChange);
        }

        // Wait until the cube falls below despawnY, with a safety timeout.
        float timeout = 20f;
        float elapsed = 0f;
        while (cube != null && elapsed < timeout)
        {
            if (cube.transform.position.y < despawnY)
                break;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (cube != null)
        {
            removing.Remove(cube);
            cubes.Remove(cube);
            Destroy(cube);
        }
        spawned = cubes.Count;
    }

    // Destroys every cube and resets the counter.
    public void ClearAllCubes()
    {
        for (int i = 0; i < cubes.Count; i++)
        {
            if (cubes[i] != null)
                Destroy(cubes[i]);
        }
        cubes.Clear();
        spawned = 0;
    }

    // Drops every cube with the fall animation so the player sees them leave.
    public void DropAllCubes()
    {
        for (int i = cubes.Count - 1; i >= 0; i--)
        {
            if (cubes[i] != null)
            {
                Rigidbody rb = cubes[i].GetComponent<Rigidbody>();
                if (rb != null)
                    rb.WakeUp();
                StartCoroutine(RemoveCubeAfterDelay(cubes[i]));
            }
        }
    }

    // Public start/stop wrappers so other scripts can control the drop loop
    // by handle rather than by string name.
    public void StartDropTimer()
    {
        StopDropTimer();
        lastDropRealTime = GetEpochSeconds();
        dropTimerRoutine = StartCoroutine(dropTimer());
    }

    public void StopDropTimer()
    {
        if (dropTimerRoutine != null)
        {
            StopCoroutine(dropTimerRoutine);
            dropTimerRoutine = null;
        }
    }

    // Drop loop that uses the real wall-clock instead of WaitForSeconds(1f).
    // When the tab wakes up after sleeping, elapsed real time is calculated and
    // dropCubes() is called once for every whole second that passed while away,
    // catching the cube count up to match the timer display immediately.
    public IEnumerator dropTimer()
    {
        while (timerScript.timerIsRunning)
        {
            yield return null; // check every frame — very cheap

            double now = GetEpochSeconds();
            double secondsSinceLastDrop = now - lastDropRealTime;

            // Fire once per real second elapsed (catch up if tab was asleep).
            if (secondsSinceLastDrop >= 1.0)
            {
                int ticksMissed = Mathf.FloorToInt((float)secondsSinceLastDrop);
                lastDropRealTime += ticksMissed; // advance anchor by whole seconds only

                // dropCubes() reads timerScript.timeRemaining which is already
                // corrected by the wall-clock in timer.cs, so a single call is
                // enough — it will drop everything that should have been dropped
                // while the tab was away in one pass.
                dropCubes();
            }
        }
        dropTimerRoutine = null;
    }

    // Returns seconds since the Unix epoch via the system clock (not Unity time).
    private double GetEpochSeconds()
    {
        return (System.DateTime.UtcNow
                - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc))
               .TotalSeconds;
    }
}