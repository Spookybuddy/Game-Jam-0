using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public int floor;
    public Vector2 mapScale;
    public int lootAmount;
    public Generation map;
    public Player player;
    private int collect;
    private int grandTotal;
    public int quota;
    private int overflow;
    public string[] upgrades;
    public string[] downgrades;
    public bool active;
    private bool warned;
    private bool doorState;
    private float countdown;
    public Transform[] doorMovements;
    public GameObject[] loot;
    private string[] upgradeOptions = new string[3];
    private string[] downgradeOptions = new string[2];
    public TextMeshProUGUI[] upgradeButtons;
    public TextMeshProUGUI[] downgradeButtons;
    private bool upPicked;
    private bool downPicked;
    private int addAmount = 0;
    public TextMeshProUGUI[] cash;
    public TextMeshProUGUI level;
    public TextMeshProUGUI results;
    private bool firstEntry;
    public GameObject rerollButton;

    public AudioSource ambient;
    public AudioSource brakes;
    public AudioSource doors;
    public AudioSource dingHigh;
    public AudioSource dingLow;

    void Update()
    {
        //Countdowns, with 10 second warning
        if (!player.paused && active) countdown -= Time.deltaTime;
        if (!warned && countdown < 10) {
            dingHigh.Play();
            warned = true;
        }

        if (countdown < 0) {
            doorState = false;
            StartCoroutine(ElevatorDoor());
        }

        //Apply changes
        if (upPicked && downPicked) {
            floor++;
            quota = Mathf.RoundToInt(quota * 1.1f);
            cash[1].text = "$" + quota;
            level.text = "Floor " + floor;
            if (addAmount != 0) map.CopyMap(addAmount);
            else map.Interiors();
            upPicked = false;
            downPicked = false;
            player.elevatorUI.SetActive(false);
            StartCoroutine(StopElevator());
        }

        //Door Animation
        for (int i = 0; i < 2; i++) {
            doorMovements[i].position = Vector3.MoveTowards(doorMovements[i].position, doorMovements[i + (doorState ? 2 : 4)].position, Time.deltaTime * 0.8f);
        }
    }

    //Advance on enter elevator
    void OnTriggerEnter(Collider other)
    {
        //Remove attached objects
        for (int i = player.transform.childCount - 1; i >= 0; i--) Destroy(player.transform.GetChild(i).gameObject);
        player.carrying = 0;

        Cursor.lockState = CursorLockMode.None;
        StartCoroutine(ElevatorDoor());
        countdown = player.baseCount;
        player.cameraLock = true;
        doorState = false;
        active = false;
        warned = false;

        player.rig.AddForce(Vector3.back * 2, ForceMode.Impulse);
    }

    //Start timer on exit elevator
    void OnTriggerExit(Collider other)
    {
        Cursor.lockState = CursorLockMode.Locked;
        countdown = player.baseCount * player.CountMulti;
        active = true;
        player.cameraLock = false;
        upPicked = false;
        downPicked = false;
    }

    //Map Reset once doors reset, player fail delete
    private IEnumerator ElevatorDoor()
    {
        if (firstEntry) doors.Play();
        yield return new WaitForSeconds(1.1f);
        if (!firstEntry) {
            firstEntry = true;
        } else {
            map.ClearMap();
            if (active || collect < quota) FailGame();
            else LevelUp();
        }
    }

    //Reloads the scene
    public void ResetGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

    //Game lost ui
    public void FailGame()
    {
        results.text = "Floor " + floor + "\nGross $" + grandTotal;
        player.failureUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
    }

    //Generates 3 upgrade choices & 2 downgrades
    public void LevelUp()
    {
        ambient.volume = 0.02f;
        overflow += (collect - quota);
        collect = 0;
        addAmount = 0;
        cash[0].text = "$0";
        Roll();
        player.elevatorUI.SetActive(true);
        player.gameUI.SetActive(false);
    }

    private IEnumerator StopElevator()
    {
        ambient.volume = 0;
        brakes.Play();
        yield return new WaitForSeconds(5);
        player.gameUI.SetActive(true);
        dingLow.Play();
        doorState = true;
    }

    //Randomize the options
    private void Roll()
    {
        if (!upPicked) {
            for (int i = 0; i < 3; i++) {
                upgradeOptions[i] = upgrades[Random.Range(0, upgrades.Length)];
                upgradeButtons[i].text = " ";
                StartCoroutine(UpAnim(i));
            }
        }
        if (!downPicked) {
            for (int i = 0; i < 2; i++) {
                downgradeOptions[i] = downgrades[Random.Range(0, downgrades.Length)];
                downgradeButtons[i].text = " ";
                StartCoroutine(DownAnim(i));
            }
        }

        rerollButton.SetActive(overflow >= 50);
    }

    //Upgrade pops in
    private IEnumerator UpAnim(int i)
    {
        yield return new WaitForSeconds((i + 1) * 0.333f);
        upgradeButtons[i].text = upgradeOptions[i];
    }

    //Upgrade pops in
    private IEnumerator DownAnim(int i)
    {
        yield return new WaitForSeconds((i + 3) * 0.5f);
        downgradeButtons[i].text = downgradeOptions[i];
    }

    //Selected upgrade
    public void UpgradeChoice(int opt)
    {
        if (upPicked) return;
        upPicked = true;
        switch (upgradeOptions[opt]) {
            case "Move Power":
                player.MovPow += 0.5f;
                break;
            case "Move Cooldown":
                player.MovCdn = Mathf.Clamp01(player.MovCdn * 0.8f);
                break;
            case "Jump Power":
                player.JmpPow += 0.5f;
                break;
            case "Jump Cooldown":
                player.JmpCdn = Mathf.Clamp01(player.JmpCdn * 0.8f);
                break;
            case "Slam Power":
                player.SlmPow += 0.5f;
                break;
            case "Slam Cooldown":
                player.SlmCdn = Mathf.Clamp01(player.SlmCdn * 0.8f);
                break;
            case "Loot Increase":
                lootAmount = Mathf.Min(lootAmount + Random.Range(5, 9), floor * 8);
                break;
            case "Time Increase":
                player.CountMulti += 0.5f;
                break;
            case "Value Multiplier":
                player.MoneyMulti += 0.5f;
                break;
            case "Quota Decrease":
                quota = Mathf.RoundToInt(quota * 0.8f);
                cash[1].text = "$" + quota;
                break;
            case "Strength Increase":
                player.WeightMulti = Mathf.Clamp01(player.WeightMulti * 0.75f);
                break;
            case "Map Smaller":
                addAmount -= 4;
                break;
        }
    }

    //Selected downgrade
    public void DowngradeChoice(int opt)
    {
        if (downPicked) return;
        downPicked = true;
        switch (downgradeOptions[opt]) {
            case "Map Large":
                addAmount += 4;
                break;
            case "Map Larger":
                addAmount += 8;
                break;
            case "Loot Reduced":
                lootAmount = Mathf.Max(lootAmount - 3, (floor + 1) * 4);
                break;
            case "Time Reduced":
                player.CountMulti = Mathf.Max(player.CountMulti - 0.1f, 0.2f);
                break;
            case "Quota Increased":
                quota = Mathf.RoundToInt(quota * 1.15f);
                cash[1].text = "$" + quota;
                break;
        }
    }

    //Starts game
    public void BeginGame()
    {
        StartCoroutine(StopElevator());
        player.elevatorUIMain.SetActive(false);
    }

    //Exit app
    public void QuitApp()
    {
        Application.Quit();
    }

    //Add items on pickup
    public void Collected(int add)
    {
        grandTotal += add;
        collect += add;
        cash[0].text = "$" + collect;
    }

    //Reroll using extra cash
    public void Reroll()
    {
        overflow -= 50;
        Roll();
    }
}