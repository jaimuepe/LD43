﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Absorber : MonoBehaviour
{

    GameManager gameManager;
    [SerializeField] float pushAngle = 15.0f;
    [SerializeField] float pushForce = 2000.0f;
    [SerializeField] float pullForce = 100.0f;
    [SerializeField] float torque = 100.0f;

    public Transform spawnPoint;

    Capturer capturer;

    bool canFire = true;
    bool firing;
    Coroutine fireCoroutine;

    float fireRatePerSecond;
    [SerializeField] float maxFireRatePerSecond;
    [SerializeField] float baseFireRatePerSecond;
    [SerializeField] float absorberRadius = 1.0f;
    [SerializeField] float absorberMaxDistance = 10.0f;

    [SerializeField] float autoDeactivateWeaponTimeInSeconds = 2.0f;
    private float autoDeactivateWeaponTimer;

    public bool ReadyToUse { get; private set; }

    bool weaponOut;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        capturer = GetComponentInChildren<Capturer>();
        animator = GetComponent<Animator>();
    }

    RaycastHit[] results = new RaycastHit[15];

    Animator animator;

    private void FixedUpdate()
    {
        if (!Input.GetButton("Fire2") || !ReadyToUse)
        {
            return;
        }

        Ray ray = new Ray(transform.position, Camera.main.transform.forward);

        Debug.DrawRay(ray.origin, absorberMaxDistance * ray.direction, Color.red);

        int ammount = Physics.SphereCastNonAlloc(ray, absorberRadius, results, absorberMaxDistance, 1 << LayerMask.NameToLayer("Minions"));
        if (ammount > 0)
        {
            for (int i = 0; i < ammount; i++)
            {
                RaycastHit hit = results[i];
                Vector3 A = hit.collider.transform.position;
                Vector3 B = ray.GetPoint(0.0f);
                Vector3 d = ray.direction.normalized;
                Vector3 v = A - B;

                Vector3 P = B + Vector3.Dot(v, d) * d;

                Vector3 dir = P - A;

                Transform t = hit.collider.transform;
                Debug.DrawRay(t.position, dir, Color.blue);

                Rigidbody rb = hit.collider.GetComponent<Rigidbody>();

                float factor = Mathf.Clamp01(1.0f - hit.distance / absorberMaxDistance);

                rb.AddTorque(120.0f * Random.insideUnitSphere * Time.deltaTime, ForceMode.Force);
                rb.MovePosition(Vector3.MoveTowards(rb.position, P, 0.01f));
                rb.AddForce((transform.position - rb.position).normalized * factor * Time.deltaTime * pullForce, ForceMode.Force);
            }
        }
    }

    void Update()
    {
        bool absorbing = Input.GetButton("Fire2");
        bool firingIsDown = Input.GetButton("Fire1");

        if (ReadyToUse && firingIsDown && gameManager.GetAmmo() > 0.0f)
        {
            if (!firing && canFire)
            {
                if (gameManager.GetAmmo() > 0)
                {
                    firing = true;
                    fireRatePerSecond = baseFireRatePerSecond;
                    StartCoroutine(RestartFireDelay());
                    fireCoroutine = StartCoroutine(Fire());
                }
                else
                {
                    firing = false;
                    fireRatePerSecond = baseFireRatePerSecond;
                    if (fireCoroutine != null) { StopCoroutine(fireCoroutine); }
                }
            }
        }
        else
        {
            firing = false;
            fireRatePerSecond = baseFireRatePerSecond;
            if (fireCoroutine != null) { StopCoroutine(fireCoroutine); }
        }

        animator.SetBool("pull", false);
        animator.SetBool("activate", false);
        animator.SetBool("deactivate", false);

        if (absorbing)
        {
            animator.SetBool("pull", true);
        }

        autoDeactivateWeaponTimer -= Time.deltaTime;
        if (ReadyToUse && autoDeactivateWeaponTimer <= 0.0f)
        {
            DeactivateWeapon();
        }

        if (ReadyToUse && (absorbing || firingIsDown))
        {
            autoDeactivateWeaponTimer = autoDeactivateWeaponTimeInSeconds;
        }
        else if (!weaponOut && (absorbing || firingIsDown))
        {
            weaponOut = true;
            animator.SetBool("activate", true);
        }
    }

    private void WeaponHidden()
    {
        weaponOut = false;
    }

    private void DeactivateWeapon()
    {
        ReadyToUse = false;
        animator.SetBool("deactivate", true);
    }

    IEnumerator RestartFireDelay()
    {
        canFire = false;
        yield return new WaitForSeconds(fireRatePerSecond);
        canFire = true;
    }

    IEnumerator Fire()
    {
        InstantiateBullet();

        while (true)
        {
            yield return new WaitForSeconds(fireRatePerSecond);
            fireRatePerSecond = Mathf.Max(maxFireRatePerSecond, fireRatePerSecond * 0.7f);
            InstantiateBullet();
        }
    }

    public void WeaponReadyToUse()
    {
        ReadyToUse = true;
        autoDeactivateWeaponTimer = autoDeactivateWeaponTimeInSeconds;
    }

    private void InstantiateBullet()
    {
        gameManager.DecreaseAmmo();

        animator.SetTrigger("throw");

        Minion newBullet = MinionsPool.Instance.Get(true);
        newBullet.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        Vector2 noise = 0.05f * Random.insideUnitCircle;
        Vector3 direction =
            Quaternion.AngleAxis(-pushAngle, spawnPoint.right) * Camera.main.transform.forward +
            Camera.main.transform.right * noise.x +
            Camera.main.transform.up * noise.y;

        newBullet.AddForce(direction * pushForce, ForceMode.Impulse);
        newBullet.AddTorque(torque * Random.insideUnitSphere, ForceMode.Impulse);
    }
}
