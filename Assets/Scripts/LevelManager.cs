using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public int level;
    private int pack;
    private int moves;
    private int best;
    private int bonus;
    private Board board;
    private GameObject canvas;
    private GameObject hUD;
    private GameObject confirmReset;
    private GameObject helpMenu;
    private GameObject settings;
    private GameObject confirmExit;
    private GameObject levelComplete;
    private bool moveMade;
    private bool completed;
    private GameObject transition;
    public float introDuration;
    public float transitionDuration;
    private float transitionTime;
    private bool transitioning;
    private int newMenu;
    private int levelToLoad;
    private int helpPage;
    private bool musicOn;
    private bool sFXOn;
    private float musicProgress;
    private AudioSource musicSource;
    private AudioSource sFXSource;
    public AudioClip musicClip;
    public AudioClip changeMenuSound;
    public AudioClip selectTileSound;
    public AudioClip moveSound;
    public AudioClip undoSound;
    public AudioClip resetSound;
    public AudioClip winSound;
    public AudioClip nextLevelSound;
    public AudioClip exitSound;

    // Start is called before the first frame update
    void Start()
    {
        moves = 0;
        board = transform.Find("Board").GetComponent<Board>();
        pack = board.GetPack();
        string colourSetup = (new string[] { board.shiftSetup, board.flipSetup, board.spinSetup, board.blinkSetup })[pack];
        string packName = (new string[] { "Shift", "Flip", "Spin", "Blink" })[pack];
        best = int.Parse(PlayerPrefs.GetString(packName + "PackBest").Split(',')[level]);
        bonus = (new string[] { board.shiftScramble, board.flipScramble, board.spinScramble, board.blinkScramble })[pack].Split(',').Length;
        int colours = 0;
        if (pack < 0 || pack > 2)
        {
            for (int i = 0; i < colourSetup.Length; i++)
            {
                int c = int.Parse(colourSetup[i] + "");
                if (colours <= c) colours = c + 1;
            }
            bonus -= colours - 1;
        }
        canvas = transform.Find("Canvas").gameObject;
        hUD = canvas.transform.Find("Panel HUD").gameObject;
        for (int i = 2; i <= 9; i++)
        {
            GameObject panel = hUD.transform.Find("Panel Colour" + i).gameObject;
            if (i == colours)
            {
                GameObject tile = Instantiate(board.tilePrefab, transform.position, transform.rotation);
                for (int j = 0; j < i; j++) panel.transform.Find("Image Tile" + (j + 1)).Find("Image Tile").GetComponent<Image>().color = tile.GetComponent<BoardTile>().colours[j];
                Destroy(tile.gameObject);
            }
            else panel.SetActive(false);
        }
        confirmReset = canvas.transform.Find("Panel ConfirmReset").gameObject;
        helpMenu = canvas.transform.Find("Panel Help").gameObject;
        settings = canvas.transform.Find("Panel Settings").gameObject;
        confirmExit = canvas.transform.Find("Panel ConfirmExit").gameObject;
        levelComplete = canvas.transform.Find("Panel LevelComplete").gameObject;
        transition = canvas.transform.Find("Panel Transition").gameObject;
        hUD.transform.Find("Image FramePreview").gameObject.SetActive(pack >= 0 && pack < 3);
        GameObject button = hUD.transform.Find("Image FrameReset").Find("Button Reset").gameObject;
        button.GetComponent<Button>().interactable = false;
        button.transform.Find("Image").gameObject.SetActive(false);
        hUD.transform.Find("Text Pack").GetComponent<TextMeshProUGUI>().text = packName + " Pack";
        hUD.transform.Find("Text Level").GetComponent<TextMeshProUGUI>().text = "Level " + (level + 1);
        hUD.transform.Find("Text Moves2").GetComponent<TextMeshProUGUI>().text = "0";
        hUD.transform.Find("Text Target1").GetComponent<TextMeshProUGUI>().text = best == 0 || bonus < best ? "Bonus:" : "Best:";
        hUD.transform.Find("Text Target2").GetComponent<TextMeshProUGUI>().text = (best == 0 || bonus < best ? bonus : best) + "";
        helpMenu.transform.Find("Text Pack").GetComponent<TextMeshProUGUI>().text = packName + " Pack Help";
        settings.transform.Find("Image FrameMusic").Find("Button Music").Find("Text State").GetComponent<TextMeshProUGUI>().text = PlayerPrefs.GetString("Music");
        settings.transform.Find("Image FrameSFX").Find("Button SFX").Find("Text State").GetComponent<TextMeshProUGUI>().text = PlayerPrefs.GetString("SFX");
        moveMade = false;
        completed = false;
        ChangeMenu(0);
        CheckMoveButtons();
        transitionTime = transitionDuration - Time.deltaTime;
        transitioning = true;
        newMenu = 0;
        levelToLoad = -1;
        helpPage = 0;
        ChangeHelpPage(0);
        musicOn = PlayerPrefs.GetString("Music") == "On";
        sFXOn = PlayerPrefs.GetString("SFX") == "On";
        musicProgress = PlayerPrefs.GetFloat("MusicProgress");
        PlayerPrefs.SetFloat("MusicProgress", 0);
        musicSource = transform.Find("Music Source").GetComponent<AudioSource>();
        sFXSource = transform.Find("SFX Source").GetComponent<AudioSource>();
        musicSource.volume = musicOn ? 1 : 0;
        sFXSource.volume = 0;
        musicSource.clip = musicClip;
        musicSource.time = musicProgress;
        musicSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (musicProgress >= musicClip.length) musicProgress = 0;
        else musicProgress += Time.deltaTime;
        if (introDuration > 0)
        {
            introDuration -= Time.deltaTime;
            if (introDuration <= 0) board.SetPreview(false);
        }
        if (transitioning)
        {
            transitionTime += Time.deltaTime * (newMenu < 0 && levelToLoad < 0 ? -1 : 1);
            Image image = transition.GetComponent<Image>();
            Color c = image.color;
            c.a = transitionTime / transitionDuration;
            image.color = c;
            if (transitionTime <= 0)
            {
                sFXSource.volume = sFXOn ? 1 : 0;
                transition.SetActive(false);
                transitioning = false;
            }
            else if (transitionTime >= transitionDuration)
            {
                if (levelToLoad >= 0)
                {
                    PlayerPrefs.SetInt("SelectedPack", pack);
                    if (completed && level % 15 == 14 && level < 59) PlayerPrefs.SetInt("SelectedPage", (level + 1) / 15);
                    PlayerPrefs.SetFloat("MusicProgress", musicProgress);
                    SceneManager.LoadScene(levelToLoad);
                }
                else if (newMenu >= 0)
                {
                    hUD.SetActive(newMenu == 0);
                    confirmReset.SetActive(newMenu == 1);
                    helpMenu.SetActive(newMenu == 2);
                    settings.SetActive(newMenu == 3);
                    confirmExit.SetActive(newMenu == 4);
                    levelComplete.SetActive(newMenu == 5);
                    newMenu = -1;
                    if (sFXOn) sFXSource.PlayOneShot(winSound, 1);
                }
            }
            return;
        }
        if (completed) return;
        if (moves != board.GetMoveCount())
        {
            if (moves < board.GetMoveCount() && sFXOn) sFXSource.PlayOneShot(moveSound, 1);
            moves = board.GetMoveCount();
            hUD.transform.Find("Text Moves2").GetComponent<TextMeshProUGUI>().text = moves + "";
            CheckMoveButtons();
            if (!moveMade)
            {
                if (!board.transform.Find("Move Stack Undo").GetComponent<MoveStack>().Empty())
                {
                    GameObject button = hUD.transform.Find("Image FrameReset").Find("Button Reset").gameObject;
                    button.GetComponent<Button>().interactable = true;
                    button.transform.Find("Image").gameObject.SetActive(true);
                    moveMade = true;
                }
            }
        }
        else return;
        completed = board.GetCompleted();
        if (!completed) return;
        board.SetActive(false);
        string packName = (new string[] { "Shift", "Flip", "Spin", "Blink" })[pack];
        levelComplete.transform.Find("Text Moves2").GetComponent<TextMeshProUGUI>().text = moves + "";
        if (best == 0 || moves < best)
        {
            if (best == 0 && level == 59)
            {
                levelComplete.transform.Find("Text LevelComplete").GetComponent<TextMeshProUGUI>().text = packName + " Pack Complete!";
                PlayerPrefs.SetInt("SelectedPage", -1);
            }
            string packBest = PlayerPrefs.GetString(packName + "PackBest");
            string[] split = packBest.Split(',');
            packBest = "";
            for (int i = 0; i < split.Length; i++) packBest += (i > 0 ? "," : "") + (i == level ? moves + "" : split[i]);
            PlayerPrefs.SetString(packName + "PackBest", packBest);
            levelComplete.transform.Find("Text OldBest1").gameObject.SetActive(false);
        }
        else
        {
            levelComplete.transform.Find("Text NewBest").gameObject.SetActive(false);
            levelComplete.transform.Find("Text OldBest2").GetComponent<TextMeshProUGUI>().text = best + "";
        }
        levelComplete.transform.Find("Text NewBonus").gameObject.SetActive(!(best > 0 && best <= bonus) && moves <= bonus);
        levelComplete.transform.Find("Text Bonus1").gameObject.SetActive(!(best > 0 && best <= bonus) && moves > bonus);
        levelComplete.transform.Find("Image Star1").gameObject.SetActive((best == 0 || best > bonus) && moves > bonus);
        levelComplete.transform.Find("Image Star2").gameObject.SetActive((best == 0 || best > bonus) && moves <= bonus);
        levelComplete.transform.Find("Image Star3").gameObject.SetActive(best > 0 && best <= bonus);
        if (best > 0 && best <= bonus)
        {

        }
        else if (moves <= bonus)
        {
            string packBonus = PlayerPrefs.GetString(packName + "PackBonus");
            string[] split = packBonus.Split(',');
            packBonus = "";
            for (int i = 0; i < split.Length; i++) packBonus += (i > 0 ? "," : "") + (i == level ? "true" : split[i]);
            PlayerPrefs.SetString(packName + "PackBonus", packBonus);
            levelComplete.transform.Find("Text Bonus1").gameObject.SetActive(false);
        }
        else levelComplete.transform.Find("Text Bonus2").GetComponent<TextMeshProUGUI>().text = bonus + "";
        newMenu = 5;
        StartTransition();
    }

    /// <summary>
    /// Change the menu panel to that with the passed index.
    /// </summary>
    /// <param name="m">The index of the new panel.</param>
    public void ChangeMenu(int m)
    {
        board.SetActive(m == 0);
        hUD.SetActive(m == 0);
        board.SetActive(m == 0);
        confirmReset.SetActive(m == 1);
        helpMenu.SetActive(m == 2);
        settings.SetActive(m == 3);
        confirmExit.SetActive(m == 4);
        levelComplete.SetActive(m == 5);
    }

    /// <summary>
    /// Set the board to preview mode.
    /// </summary>
    public void Preview()
    {
        board.SetPreview(true);
    }

    /// <summary>
    /// Undo the most recently made move.
    /// </summary>
    public void Undo()
    {
        if (completed) return;
        board.Undo();
        CheckMoveButtons();
        if (sFXOn) sFXSource.PlayOneShot(undoSound, 1);
    }

    /// <summary>
    /// Redo the most recently undone move.
    /// </summary>
    public void Redo()
    {
        if (completed) return;
        board.Redo();
        CheckMoveButtons();
    }

    /// <summary>
    /// Check whether the board can currently be undone/redone and set the interactibility status of buttons accordingly.
    /// </summary>
    private void CheckMoveButtons()
    {
        GameObject button = hUD.transform.Find("Image FrameUndo").Find("Button Undo").gameObject;
        bool b = !board.transform.Find("Move Stack Undo").GetComponent<MoveStack>().Empty();
        button.GetComponent<Button>().interactable = b;
        button.transform.Find("Image").gameObject.SetActive(b);
        button = hUD.transform.Find("Image FrameRedo").Find("Button Redo").gameObject;
        b = !board.transform.Find("Move Stack Redo").GetComponent<MoveStack>().Empty();
        button.GetComponent<Button>().interactable = b;
        button.transform.Find("Image").gameObject.SetActive(b);
    }

    /// <summary>
    /// Reload the scene.
    /// </summary>
    public void Reset()
    {
        LoadLevel(SceneManager.GetActiveScene().buildIndex);
        if (sFXOn) sFXSource.PlayOneShot(resetSound, 1);
    }

    /// <summary>
    /// Switch music status on/off.
    /// </summary>
    public void ToggleMusic()
    {
        musicOn = !musicOn;
        string newMusic = musicOn ? "On" : "Off";
        PlayerPrefs.SetString("Music", newMusic);
        settings.transform.Find("Image FrameMusic").Find("Button Music").Find("Text State").GetComponent<TextMeshProUGUI>().text = newMusic;
        musicSource.gameObject.SetActive(musicOn);
    }

    /// <summary>
    /// Switch SFX status on/off.
    /// </summary>
    public void ToggleSFX()
    {
        sFXOn = !sFXOn;
        string newSFX = sFXOn ? "On" : "Off";
        PlayerPrefs.SetString("SFX", newSFX);
        settings.transform.Find("Image FrameSFX").Find("Button SFX").Find("Text State").GetComponent<TextMeshProUGUI>().text = newSFX;
        sFXSource.gameObject.SetActive(sFXOn);
    }

    /// <summary>
    /// Change the selected page on the help menu.
    /// </summary>
    /// <param name="increment">The amount by which to change.</param>
    public void ChangeHelpPage(int increment)
    {
        helpPage += increment;
        string[] packs = new string[] { "ShiftPack", "FlipPack", "SpinPack", "BlinkPack" };
        for (int i = 0; i < 4; i++)
        {
            GameObject panel = helpMenu.transform.Find("Panel " + packs[i]).gameObject;
            panel.SetActive(pack == i);
            if (pack == i) for (int j = 0; j < 4; j++) panel.transform.Find("Panel Page" + (j + 1)).gameObject.SetActive(helpPage == j);
        }
        helpMenu.transform.Find("Text Page").GetComponent<TextMeshProUGUI>().text = (helpPage + 1) + "/4";
        GameObject button = helpMenu.transform.Find("Image FramePreviousPage").Find("Button PreviousPage").gameObject;
        button.GetComponent<Button>().interactable = helpPage > 0;
        button.transform.Find("Image").gameObject.SetActive(helpPage > 0);
        button = helpMenu.transform.Find("Image FrameNextPage").Find("Button NextPage").gameObject;
        button.GetComponent<Button>().interactable = helpPage < 3;
        button.transform.Find("Image").gameObject.SetActive(helpPage < 3);
    }

    /// <summary>
    /// Load the next level in the pack.
    /// </summary>
    public void NextLevel()
    {
        LoadLevel(SceneManager.GetActiveScene().buildIndex + 1);
        if (sFXOn) sFXSource.PlayOneShot(nextLevelSound, 1);
    }

    /// <summary>
    /// Return to the main menu.
    /// </summary>
    public void Exit()
    {
        LoadLevel(0);
        if (sFXOn) sFXSource.PlayOneShot(exitSound, 1);
    }

    /// <summary>
    /// Load the scene with the passed index.
    /// </summary>
    /// <param name="l">The index of the new scene.</param>
    private void LoadLevel(int l)
    {
        levelToLoad = l;
        StartTransition();
    }

    /// <summary>
    /// Begin transitioning to a new panel/scene.
    /// </summary>
    private void StartTransition()
    {
        transition.SetActive(true);
        transitionTime = 0;
        transitioning = true;
    }
}
