using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Player Systems")]
    public Transform orbital;
    public Rigidbody rig;
    public GameManager manager;
    public float basePow;
    public float baseCdn;
    public float baseJump;
    public float baseSlam;
    public float baseCount;

    public GameObject pauseUI;
    public GameObject failureUI;
    public GameObject elevatorUIMain;
    public GameObject elevatorUI;
    public GameObject gameUI;

    public int carrying;

    private float countF;
    private float countB;
    private float countL;
    private float countR;
    private float countJ;
    private float countS;

    public Vector2 cameraDelta;
    public bool cameraLock;

    [Header("Settings")]
    public float rotationSpd;
    public float volume;
    public bool paused;

    [Header("Roguelike Player Stats")]
    public float MovPow;
    public float MovCdn;
    public float JmpPow;
    public float JmpCdn;
    public float SlmPow;
    public float SlmCdn;
    public float CountMulti;
    public float MoneyMulti;
    public float WeightMulti;

    void Update()
    {
        if (paused) {
            if (!manager.active) {
                paused = false;
                Time.timeScale = 1;
            }
            else pauseUI.SetActive(true);
        } else {
            if (cameraLock) {
                orbital.transform.eulerAngles = Vector3.RotateTowards(Vector3.up, Vector3.zero, Time.deltaTime, 0);
                cameraDelta = Vector2.zero;
            } else {
                if (cameraDelta.x != 0) {
                    orbital.transform.Rotate(cameraDelta.x * Vector3.up * rotationSpd);
                    cameraDelta = Vector2.MoveTowards(cameraDelta, Vector2.zero, Time.deltaTime * 2);
                }
            }

            pauseUI.SetActive(false);
            orbital.position = transform.position;
            //Cooldowns
            if (countF > 0) countF -= Time.deltaTime;
            if (countB > 0) countB -= Time.deltaTime;
            if (countL > 0) countL -= Time.deltaTime;
            if (countR > 0) countR -= Time.deltaTime;
            if (countJ > 0) countJ -= Time.deltaTime;
            if (countS > 0) countS -= Time.deltaTime;
        }
    }

    //Squash & stretch & item pickup
    void OnCollisionEnter(Collision hit)
    {
        if (hit.gameObject.CompareTag("Loot")) {
            hit.transform.parent = transform;
            Destroy(hit.gameObject.GetComponent<Collider>());
            manager.Collected(Mathf.RoundToInt(hit.gameObject.GetComponent<Item>().value * MoneyMulti));
            carrying += Mathf.RoundToInt(hit.gameObject.GetComponent<Item>().weight * WeightMulti);
        }
    }

    //Rotate the camera around
    public void Camera(InputAction.CallbackContext ctx)
    {
        if (!cameraLock && ctx.performed) cameraDelta = ctx.ReadValue<Vector2>();
    }

    //Reset the camera rotation to the direction of the elevator
    public void CameraReset(InputAction.CallbackContext ctx)
    {

    }

    //Force relative to camera front; Power var MovPow | Cooldown var MovCdn
    public void Forward(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && countF <= 0) {
            rig.AddForce(orbital.forward * basePow * MovPow, ForceMode.Impulse);
            countF = MovCdn * baseCdn;
        }
    }

    //Force relative to camera back; Power var MovPow | Cooldown var MovCdn
    public void Backward(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && countB <= 0) {
            rig.AddForce(orbital.forward * -basePow * MovPow, ForceMode.Impulse);
            countB = MovCdn * baseCdn;
        }
    }

    //Force relative to camera left; Power var MovPow | Cooldown var MovCdn
    public void Leftward(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && countL <= 0) {
            rig.AddForce(orbital.right * -basePow * MovPow, ForceMode.Impulse);
            countL = MovCdn * baseCdn;
        }
    }

    //Force relative to camera right; Power var MovPow | Cooldown var MovCdn
    public void Rightward(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && countR <= 0) {
            rig.AddForce(orbital.right * basePow * MovPow, ForceMode.Impulse);
            countR = MovCdn * baseCdn;
        }
    }

    //Force upward; Power var JmpPow | Cooldown var JmpCdn
    public void Jump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && countJ <= 0) {
            rig.AddForce(Vector3.up * baseJump * JmpPow, ForceMode.Impulse);
            countJ = JmpCdn * baseCdn;
        }
    }

    //Force downward; Power var SlmPow | Cooldown var SlmCdn
    public void Slam(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && countS <= 0) {
            rig.AddForce(Vector3.down * baseSlam * SlmPow, ForceMode.Impulse);
            countS = SlmCdn * baseCdn;
        }
    }

    //Toggle the pause state of the game
    public void Pause(InputAction.CallbackContext ctx)
    {
        paused = !paused;
        Time.timeScale = paused ? 0 : 1;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    //Force pause state
    public void ForcePause()
    {
        paused = true;
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
    }

    //Unpasue
    public void ForceUnpause()
    {
        paused = false;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
    }
}