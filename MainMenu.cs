using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    private int selectedPack;
    private int[] selectedPages;
    public int levelsPerPage;
    private int[] packProgress;
    private int[] packStars;
    private bool[] shiftPackStars;
    private bool[] flipPackStars;
    private bool[] blinkPackStars;
    private bool[] spinPackStars;
    private GameObject canvas;
    private GameObject startMenu;
    private GameObject packMenu;
    private GameObject levelMenu;
    private GameObject settings1;
    private GameObject settings2;
    private GameObject helpMenu;
    private GameObject transition;
    public float transitionDuration;
    private float transitionTime;
    private bool transitioning;
    private int newMenu;
    private int levelToLoad;
    private int helpPage;
    private GameObject[] titleTiles;
    public int titleTileCount;
    private bool darkTheme;
    private bool musicOn;
    private bool sFXOn;
    private float musicProgress;
    private AudioSource musicSource;
    private AudioSource sFXSource;
    public AudioClip musicClip;
    public AudioClip changeMenuSound;
    public AudioClip menuTransitionSound;
    public AudioClip loadLevelSound;
    public GameObject tilePrefab;

    // Start is called before the first frame update
    void Start()
    {
        if (!PlayerPrefs.HasKey("SelectedPack")) InitialiseData();
        selectedPages = new int[4];
        selectedPack = PlayerPrefs.GetInt("SelectedPack");
        newMenu = selectedPack < 0 ? 0 : PlayerPrefs.GetInt("SelectedPage") < 0 ? 1 : 2;
        for (int i = 0; i < selectedPages.Length; i++) selectedPages[i] = i == selectedPack ? PlayerPrefs.GetInt("SelectedPage") : -1;
        if (selectedPack < 0) selectedPack = 0;
        PlayerPrefs.SetInt("SelectedPack", -1);
        PlayerPrefs.SetInt("SelectedPage", -1);
        packProgress = new int[4];
        packStars = new int[4];
        shiftPackStars = new bool[60];
        flipPackStars = new bool[60];
        spinPackStars = new bool[60];
        blinkPackStars = new bool[60];
        string[] packNames = new string[]{ "ShiftPack", "FlipPack", "SpinPack", "BlinkPack" };
        for (int i = 0; i < selectedPages.Length; i++)
        {
            string[] packBest = PlayerPrefs.GetString(packNames[i] + "Best").Split(',');
            string[] packBonus = PlayerPrefs.GetString(packNames[i] + "Bonus").Split(',');
            while (packProgress[i] < 60 && packBest[packProgress[i]] != "0") packProgress[i]++;
            for (int j = 0; j < packProgress[i]; j++)
            {
                if (bool.Parse(packBonus[j]))
                {
                    packStars[i]++;
                    if (i == 0) shiftPackStars[j] = true;
                    if (i == 1) flipPackStars[j] = true;
                    if (i == 2) spinPackStars[j] = true;
                    else blinkPackStars[j] = true;
                }
            }
            if (selectedPages[i] < 0) selectedPages[i] = packProgress[i] == 60 ? 0 : packProgress[i] / levelsPerPage;
        }
        canvas = transform.Find("Canvas").gameObject;
        startMenu = canvas.transform.Find("Panel Start").gameObject;
        packMenu = canvas.transform.Find("Panel Packs").gameObject;
        levelMenu = canvas.transform.Find("Panel Levels").gameObject;
        settings1 = canvas.transform.Find("Panel Settings1").gameObject;
        settings2 = canvas.transform.Find("Panel Settings2").gameObject;
        helpMenu = canvas.transform.Find("Panel Help").gameObject;
        transition = canvas.transform.Find("Panel Transition").gameObject;
        titleTiles = new GameObject[titleTileCount];
        for (int i = 0; i < titleTiles.Length; i++)
        {
            float f = 1 - (i * 1.0f / titleTiles.Length);
            GameObject t = Instantiate(tilePrefab, transform.position + new Vector3(0, 0.5f, 0), transform.rotation);
            t.transform.localScale = new Vector3(f * 1.5f, f * 1.5f, 1);
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            Color c = sr.color;
            c.a = (i + 1) * 1f / titleTiles.Length;
            sr.color = c;
            titleTiles[i] = t;
        }
        levelMenu.transform.Find("Image FramePreviousPage").Find("Button PreviousPage").GetComponent<Button>().interactable = false;
        settings1.transform.Find("Image FrameTheme").Find("Button Theme").Find("Text State").GetComponent<TextMeshProUGUI>().text = PlayerPrefs.GetString("DarkTheme");
        settings1.transform.Find("Image FrameMusic").Find("Button Music").Find("Text State").GetComponent<TextMeshProUGUI>().text = PlayerPrefs.GetString("Music");
        settings1.transform.Find("Image FrameSFX").Find("Button SFX").Find("Text State").GetComponent<TextMeshProUGUI>().text = PlayerPrefs.GetString("SFX");
        settings2.transform.Find("Image FrameTheme").Find("Button Theme").Find("Text State").GetComponent<TextMeshProUGUI>().text = PlayerPrefs.GetString("DarkTheme");
        settings2.transform.Find("Image FrameMusic").Find("Button Music").Find("Text State").GetComponent<TextMeshProUGUI>().text = PlayerPrefs.GetString("Music");
        settings2.transform.Find("Image FrameSFX").Find("Button SFX").Find("Text State").GetComponent<TextMeshProUGUI>().text = PlayerPrefs.GetString("SFX");
        ChangePack(0);
        transitionTime = transitionDuration - Time.deltaTime;
        transitioning = true;
        levelToLoad = -1;
        helpPage = 0;
        ChangeHelpPage(0);
        darkTheme = PlayerPrefs.GetString("DarkTheme") == "On";
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
                    PlayerPrefs.SetFloat("MusicProgress", musicProgress);
                    SceneManager.LoadScene(levelToLoad);
                }
                else if (newMenu >= 0)
                {
                    ChangeMenu(newMenu);
                    newMenu = -1;
                }
            }
        }
    }

    /// <summary>
    /// Set up player preferences the first time the player opens the game.
    /// </summary>
    private void InitialiseData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("SelectedPack", -1);
        PlayerPrefs.SetInt("SelectedPage", 0);
        PlayerPrefs.SetString("DarkTheme", "Off");
        PlayerPrefs.SetString("Music", "On");
        PlayerPrefs.SetString("SFX", "On");
        PlayerPrefs.SetFloat("MusicProgress", 0);
        string s = "0";
        for (int i = 1; i < 60; i++) s += ",0";
        PlayerPrefs.SetString("ShiftPackBest", s);
        PlayerPrefs.SetString("FlipPackBest", s);
        PlayerPrefs.SetString("SpinPackBest", s);
        PlayerPrefs.SetString("BlinkPackBest", s);
        s = "false";
        for (int i = 1; i < 60; i++) s += ",false";
        PlayerPrefs.SetString("ShiftPackBonus", s);
        PlayerPrefs.SetString("FlipPackBonus", s);
        PlayerPrefs.SetString("SpinPackBonus", s);
        PlayerPrefs.SetString("BlinkPackBonus", s);
    }

    /// <summary>
    /// Change the menu panel to that with the passed index.
    /// </summary>
    /// <param name="m">The index of the new panel.</param>
    public void ChangeMenu(int m)
    {
        startMenu.SetActive(m == 0);
        foreach (GameObject t in titleTiles) t.SetActive(m == 0);
        packMenu.SetActive(m == 1);
        levelMenu.SetActive(m == 2);
        settings1.SetActive(m == 3);
        settings2.SetActive(m == 4);
        helpMenu.SetActive(m == 5);
        if (sFXOn) sFXSource.PlayOneShot(changeMenuSound, 1);
    }

    /// <summary>
    /// Change the menu panel with a transition.
    /// </summary>
    /// <param name="m">The index of the new panel.</param>
    public void ChangeMenuTransition(int m)
    {
        newMenu = m;
        StartTransition();
        if (sFXOn) sFXSource.PlayOneShot(menuTransitionSound, 1);
    }

    /// <summary>
    /// Change the selected pack.
    /// </summary>
    /// <param name="increment">The amount by which to change.</param>
    public void ChangePack(int increment)
    {
        selectedPack += increment + 4;
        selectedPack %= 4;
        string pack = (new string[] { "Shift Pack", "Flip Pack", "Spin Pack", "Blink Pack" })[selectedPack];
        GameObject packButton = packMenu.transform.Find("Image FramePack").Find("Button Pack").gameObject;
        packButton.transform.Find("Text Pack").GetComponent<TextMeshProUGUI>().text = pack;
        packButton.transform.Find("Text Progress2").GetComponent<TextMeshProUGUI>().text = packProgress[selectedPack] + "/60";
        packButton.transform.Find("Text Stars2").GetComponent<TextMeshProUGUI>().text = packStars[selectedPack] + "/60";
        levelMenu.transform.Find("Text Pack").GetComponent<TextMeshProUGUI>().text = pack;
        levelMenu.transform.Find("Image Star").Find("Text Stars").GetComponent<TextMeshProUGUI>().text = packStars[selectedPack] + "/60";
        helpMenu.transform.Find("Text Pack").GetComponent<TextMeshProUGUI>().text = pack + " Help";
        helpPage = 0;
        ChangePage(0);
    }

    /// <summary>
    /// Change the selected page on the level menu.
    /// </summary>
    /// <param name="increment">The amount by which to change.</param>
    public void ChangePage(int increment)
    {
        selectedPages[selectedPack] += increment;
        GameObject frame = levelMenu.transform.Find("Image FramePreviousPage").gameObject;
        frame.SetActive(selectedPages[selectedPack] > 0);
        GameObject button = frame.transform.Find("Button PreviousPage").gameObject;
        bool b = selectedPages[selectedPack] > 0;
        button.GetComponent<Button>().interactable = b;
        button.transform.Find("Image").gameObject.SetActive(b);
        frame = levelMenu.transform.Find("Image FrameNextPage").gameObject;
        frame.SetActive(selectedPages[selectedPack] < 3);
        button = frame.transform.Find("Button NextPage").gameObject;
        b = (selectedPages[selectedPack] + 1) * levelsPerPage <= packProgress[selectedPack] && (selectedPages[selectedPack] + 1) * levelsPerPage < 60;
        button.GetComponent<Button>().interactable = b;
        button.transform.Find("Image").gameObject.SetActive(b);
        CheckLevels();
        ChangeHelpPage(0);
    }

    /// <summary>
    /// Set the number/accessibility of each level on the current page.
    /// </summary>
    private void CheckLevels()
    {
        int firstLevel = selectedPages[selectedPack] * levelsPerPage;
        for (int i = 0; i < levelsPerPage; i++)
        {
            GameObject levelFrame = levelMenu.transform.Find("Image FrameLevel" + (i + 1)).gameObject;
            GameObject levelButton = levelFrame.transform.Find("Button Level").gameObject;
            bool completed = firstLevel + i < packProgress[selectedPack];
            if (completed)
            {
                bool[] currentStars = (new bool[][] { shiftPackStars, flipPackStars, spinPackStars, blinkPackStars })[selectedPack];
                levelButton.transform.Find("Image Star").Find("Image Star").gameObject.SetActive(currentStars[firstLevel + i]);
            }
            else
            {
                levelButton.transform.Find("Image Star").gameObject.SetActive(false);
                levelButton.GetComponent<Button>().interactable = firstLevel + i <= packProgress[selectedPack];
            }
            GameObject levelText = levelButton.transform.Find("Text Level").gameObject;
            levelText.GetComponent<TextMeshProUGUI>().text = firstLevel + i <= packProgress[selectedPack] ? (firstLevel + i + 1) + "" : "";
        }
    }

    /// <summary>
    /// Switch between light and dark theme.
    /// </summary>
    public void ToggleTheme()
    {
        darkTheme = !darkTheme;
        string newDarkTheme = darkTheme ? "On" : "Off";
        PlayerPrefs.SetString("DarkTheme", newDarkTheme);
        settings1.transform.Find("Image FrameTheme").Find("Button Theme").Find("Text State").GetComponent<TextMeshProUGUI>().text = newDarkTheme;
        settings2.transform.Find("Image FrameTheme").Find("Button Theme").Find("Text State").GetComponent<TextMeshProUGUI>().text = newDarkTheme;
        if (sFXOn) sFXSource.PlayOneShot(changeMenuSound, 1);
    }

    /// <summary>
    /// Switch music status on/off.
    /// </summary>
    public void ToggleMusic()
    {
        musicOn = !musicOn;
        string newMusic = musicOn ? "On" : "Off";
        PlayerPrefs.SetString("Music", newMusic);
        settings1.transform.Find("Image FrameMusic").Find("Button Music").Find("Text State").GetComponent<TextMeshProUGUI>().text = newMusic;
        settings2.transform.Find("Image FrameMusic").Find("Button Music").Find("Text State").GetComponent<TextMeshProUGUI>().text = newMusic;
        musicSource.volume = musicOn ? 1 : 0;
        if (sFXOn) sFXSource.PlayOneShot(changeMenuSound, 1);
    }

    /// <summary>
    /// Switch sound effect status on/off.
    /// </summary>
    public void ToggleSFX()
    {
        sFXOn = !sFXOn;
        string newSFX = sFXOn ? "On" : "Off";
        PlayerPrefs.SetString("SFX", newSFX);
        settings1.transform.Find("Image FrameSFX").Find("Button SFX").Find("Text State").GetComponent<TextMeshProUGUI>().text = newSFX;
        settings2.transform.Find("Image FrameSFX").Find("Button SFX").Find("Text State").GetComponent<TextMeshProUGUI>().text = newSFX;
        sFXSource.volume = sFXOn ? 1 : 0;
        if (sFXOn) sFXSource.PlayOneShot(changeMenuSound, 1);
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
            panel.SetActive(selectedPack == i);
            if (selectedPack == i) for (int j = 0; j < 4; j++) panel.transform.Find("Panel Page" + (j + 1)).gameObject.SetActive(helpPage == j);
        }
        helpMenu.transform.Find("Text Page").GetComponent<TextMeshProUGUI>().text = (helpPage + 1) + "/4";
        GameObject button = helpMenu.transform.Find("Image FramePreviousPage").Find("Button PreviousPage").gameObject;
        button.GetComponent<Button>().interactable = helpPage > 0;
        button.transform.Find("Image").gameObject.SetActive(helpPage > 0);
        button = helpMenu.transform.Find("Image FrameNextPage").Find("Button NextPage").gameObject;
        button.GetComponent<Button>().interactable = helpPage < 3;
        button.transform.Find("Image").gameObject.SetActive(helpPage < 3);
        if (sFXOn) sFXSource.PlayOneShot(changeMenuSound, 1);
    }

    /// <summary>
    /// Load the level on the current page with the passed index.
    /// </summary>
    /// <param name="l">The level index.</param>
    public void LoadLevel(int l)
    {
        PlayerPrefs.SetInt("SelectedPack", selectedPack);
        PlayerPrefs.SetInt("SelectedPage", selectedPages[selectedPack]);
        levelToLoad = selectedPages[selectedPack] * levelsPerPage + l;
        StartTransition();
        if (sFXOn) sFXSource.PlayOneShot(loadLevelSound, 1);
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
