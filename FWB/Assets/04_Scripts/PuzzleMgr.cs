using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static AbilityTable;
using static ChipTable;

/// <summary>
/// 퍼즐 플레이에 관련된 기능을 관리
/// </summary>
public class PuzzleMgr : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    public GridLayoutGroup puzzleGrid;
    public RectTransform chipPanelRectTr;
    public EventSystem es;
    public Canvas canvas;
    public RawImage dragImg;
    public Text creditText;
    public Text orderText;
    public Text bluePrintName;
    public Text currentAbilityText;
    public Text requiredAbilityText;
    public Text usedChipText;
    public Text selectedChipName;
    public Text selectedChipPrice;
    public Text selectedChipDesc;
    public float chipSize;
    public PuzzleFrame puzzleFrame;
    public List<Texture> textureList = new List<Texture>();
    public List<ChipObj> chipList = new List<ChipObj>();
    public List<AbilityFilterUI> abilityFilterList = new List<AbilityFilterUI>();
    public List<AbilityFilterUI> abilityTagList = new List<AbilityFilterUI>();
    public Button makingDone;
    public Button sortTargetBtn;
    public Button sortOrderBtn;
    public Button revertChips;
    public GameSceneMgr mgr2;
    public bool isFeverMode;
    public Action<int> OnMakingDone;
    [HideInInspector]
    public bool isTutorial = true;

    private RectTransform dragImgRectTr;
    private Puzzle currPuzzle;
    private ChipObj currentSelectedChip;
    private Chip currentSelectedChipData;
    private CanvasScaler canvasScaler;
    private PointerEventData ped;
    private bool isFromPuzzle;
    private bool isAscending;
    private int enabledFrameCnt;
    private int pressedChipIndex = -1;
    private Vector3 resolutionOffset;
    private Vector3 chipOffset;
    private Vector3 prevMousePos;
    private SpriteChange sortOrderSC;
    private List<PuzzleFrame> puzzleFrameList = new List<PuzzleFrame>();
    private List<string> filterAbilityKeyList = new List<string>();
    private List<string> creatableChipKeyList = new List<string>();
    private Dictionary<Chip, int> currentChipInPuzzleDic = new Dictionary<Chip, int>();
    private Dictionary<Ability, int> currentAbilityInPuzzleDic = new Dictionary<Ability, int>();
    private Dictionary<ChipObj, int> chipObjDic = new Dictionary<ChipObj, int>();

    private float lastRotationTime = 0f;
    private float rotationDebounceTime = 0.01f; // 디바운싱 시간 설정
    public class Puzzle
    {
        public int x;
        public int y;
        public List<Ability> requiredChipAbilityList = new List<Ability>();
        public PuzzleFrameData[,] frameDataTable;
    }

    [System.Serializable]
    public class PuzzleFrameData
    {
        public int patternNum = 0;
        public int x;
        public int y;
        public ChipObj chip;

        public PuzzleFrameData(int patternNum, int x, int y)
        {
            this.patternNum = patternNum;
            this.x = x;
            this.y = y;
        }
    }

    void Awake()
    {
        makingDone.onClick.AddListener(OnClickMakingDone);
        sortTargetBtn.onClick.AddListener(OnClickSortTarget);
        sortOrderBtn.onClick.AddListener(OnClickSortTarget);
        revertChips.onClick.AddListener(OnClickRevertChips);

        dragImgRectTr = dragImg.GetComponent<RectTransform>();
        canvasScaler = canvas.GetComponent<CanvasScaler>();
        ped = new PointerEventData(es);
        sortOrderSC = sortOrderBtn.GetComponent<SpriteChange>();

        resolutionOffset = new Vector3(canvasScaler.referenceResolution.x, canvasScaler.referenceResolution.y, 0) / 2;

        for (int i = 0; i < abilityFilterList.Count; i++)
        {
            int tempNum = i;
            abilityFilterList[tempNum].button.onClick.AddListener(() =>
            {
                var filter = abilityFilterList[tempNum];
                if (filter.isOn)
                {
                    filter.image.sprite = filter.offSprite;
                    filterAbilityKeyList.Remove(filter.abilityKey);

                    var tag = abilityTagList.Find(x => x.abilityKey.Equals(filter.abilityKey));
                    tag.abilityKey = string.Empty;
                    tag.transform.SetSiblingIndex(2);
                    tag.image.sprite = tag.offSprite;
                    tag.textForTag.text = string.Empty;
                    tag.button.interactable = false;
                    tag.button.onClick.RemoveAllListeners();
                }
                else
                {
                    if (filterAbilityKeyList.Count >= 3)
                    {
                        return;
                    }
                    filter.image.sprite = filter.onSprite;
                    filterAbilityKeyList.Add(filter.abilityKey);

                    var tag = abilityTagList.First(x => string.IsNullOrEmpty(x.abilityKey));
                    tag.abilityKey = filter.abilityKey;
                    tag.transform.SetSiblingIndex(filterAbilityKeyList.Count - 1);
                    tag.image.sprite = tag.onSprite;
                    tag.textForTag.text = GameMgr.In.GetAbility(filter.abilityKey).name;
                    tag.button.interactable = true;
                    tag.button.onClick.AddListener(() =>
                    {
                        filter.button.onClick.Invoke();
                    });
                }
                filter.isOn = !filter.isOn;

                foreach (var chip in chipList)
                {
                    if (!creatableChipKeyList.Contains(chip.chipKey))
                    {
                        continue;
                    }

                    bool isActive = true;
                    var chipData = GameMgr.In.GetChip(chip.chipKey);
                    foreach (var filterAbilityKey in filterAbilityKeyList)
                    {
                        var targetAbility = chipData.abilityList.Find(x => x.abilityKey.Equals(filterAbilityKey));
                        if (targetAbility == null)
                        {
                            isActive = false;
                            break;
                        }
                    }
                    chip.backgroundSC.gameObject.SetActive(isActive);
                }
            });
        }
    }

    void Update()
    {
        if (currentSelectedChip != null)
        {
            if (Input.GetKeyUp(KeyCode.Mouse1) || Input.GetKeyDown(KeyCode.R) && Time.time - lastRotationTime > rotationDebounceTime)
            {
                lastRotationTime = Time.time;
                var angle = dragImgRectTr.localEulerAngles;
                chipOffset = Vector3.zero;
                if (angle.z < 1)
                {
                    angle.z = 270;
                    chipOffset = new Vector3((dragImgRectTr.sizeDelta.x + dragImgRectTr.sizeDelta.y) / 2,
                                             (-dragImgRectTr.sizeDelta.y + dragImgRectTr.sizeDelta.x) / 2);
                }
                else if (angle.z < 91)
                {
                    angle.z = 0;
                    chipOffset = new Vector3((dragImgRectTr.sizeDelta.y - dragImgRectTr.sizeDelta.x) / 2,
                                             (dragImgRectTr.sizeDelta.x + dragImgRectTr.sizeDelta.y) / 2);
                }
                else if (angle.z < 181)
                {
                    angle.z = 90;
                    chipOffset = new Vector3((-dragImgRectTr.sizeDelta.x - dragImgRectTr.sizeDelta.y) / 2,
                                             (dragImgRectTr.sizeDelta.y - dragImgRectTr.sizeDelta.x) / 2);
                }
                else if (angle.z < 271)
                {
                    angle.z = 180;
                    chipOffset = new Vector3((-dragImgRectTr.sizeDelta.y + dragImgRectTr.sizeDelta.x) / 2,
                                             (-dragImgRectTr.sizeDelta.x - dragImgRectTr.sizeDelta.y) / 2);
                }
                dragImgRectTr.localEulerAngles = angle;
                dragImgRectTr.localPosition += chipOffset;

                currentSelectedChip.RotateRight();
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A)
         || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X))
        {
            if (pressedChipIndex == -1)
            {
                int keyCnt = 0;
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    pressedChipIndex = 0;
                    keyCnt++;
                }
                if (Input.GetKeyDown(KeyCode.W))
                {
                    pressedChipIndex = 1;
                    keyCnt++;
                }
                if (Input.GetKeyDown(KeyCode.A))
                {
                    pressedChipIndex = 2;
                    keyCnt++;
                }
                if (Input.GetKeyDown(KeyCode.S))
                {
                    pressedChipIndex = 3;
                    keyCnt++;
                }
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    pressedChipIndex = 4;
                    keyCnt++;
                }
                if (Input.GetKeyDown(KeyCode.X))
                {
                    pressedChipIndex = 5;
                    keyCnt++;
                }

                if (keyCnt == 1)
                {
                    currentSelectedChip = null;

                    if (pressedChipIndex >= 0 && chipList.Count > pressedChipIndex)
                    {
                        ChipObj targetChip = chipList[pressedChipIndex];
                        if (targetChip == null || !targetChip.gameObject.activeInHierarchy)
                        {
                            currentSelectedChipData = null;
                            currentSelectedChip = null;
                        }
                        else
                        {
                            isFromPuzzle = false;

                            currentSelectedChipData = GameMgr.In.GetChip(targetChip.chipKey);
                            currentSelectedChip = targetChip;

                            currentSelectedChip.SaveCurrentRow();

                            selectedChipName.text = currentSelectedChipData.chipName;
                            selectedChipPrice.text = "개당 " + currentSelectedChipData.price + " c";
                            selectedChipDesc.text = currentSelectedChipData.desc;

                            dragImg.texture = currentSelectedChip.image.texture;
                            if (currentSelectedChip.originRow.Length == currentSelectedChip.rowNum)
                            {
                                dragImgRectTr.sizeDelta = new Vector2(currentSelectedChip.rowNum, currentSelectedChip.colNum) * chipSize;
                            }
                            else
                            {
                                dragImgRectTr.sizeDelta = new Vector2(currentSelectedChip.colNum, currentSelectedChip.rowNum) * chipSize;
                            }

                            chipOffset = Vector3.zero;
                            dragImgRectTr.localEulerAngles = currentSelectedChip.rectTr.localEulerAngles;
                            var angle = dragImgRectTr.localEulerAngles;
                            if (angle.z < 1)
                            {
                                chipOffset = new Vector3((-dragImgRectTr.sizeDelta.x) / 2, (dragImgRectTr.sizeDelta.y) / 2);
                            }
                            else if (angle.z < 91)
                            {
                                chipOffset = new Vector3((-dragImgRectTr.sizeDelta.x) / 2, (-dragImgRectTr.sizeDelta.y) / 2);
                            }
                            else if (angle.z < 181)
                            {
                                chipOffset = new Vector3((dragImgRectTr.sizeDelta.x) / 2, (-dragImgRectTr.sizeDelta.y) / 2);
                            }
                            else if (angle.z < 271)
                            {
                                chipOffset = new Vector3((dragImgRectTr.sizeDelta.x) / 2, (dragImgRectTr.sizeDelta.y) / 2);
                            }
                            dragImgRectTr.localPosition = Input.mousePosition + chipOffset - resolutionOffset;

                            dragImg.gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    pressedChipIndex = -1;
                }
            }
        }

        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A)
         || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.X))
        {
            var pressedChipIndexList = new List<int>();
            if (Input.GetKey(KeyCode.Q))
            {
                pressedChipIndexList.Add(0);
            }
            if (Input.GetKey(KeyCode.W))
            {
                pressedChipIndexList.Add(1);
            }
            if (Input.GetKey(KeyCode.A))
            {
                pressedChipIndexList.Add(2);
            }
            if (Input.GetKey(KeyCode.S))
            {
                pressedChipIndexList.Add(3);
            }
            if (Input.GetKey(KeyCode.Z))
            {
                pressedChipIndexList.Add(4);
            }
            if (Input.GetKey(KeyCode.X))
            {
                pressedChipIndexList.Add(5);
            }

            if (pressedChipIndexList.Contains(pressedChipIndex) && currentSelectedChip != null)
            {
                if (prevMousePos == Vector3.zero)
                {
                    dragImgRectTr.localPosition = Input.mousePosition + chipOffset - resolutionOffset;
                }
                else
                {
                    var delta = Input.mousePosition - prevMousePos;
                    dragImgRectTr.localPosition += delta / canvas.scaleFactor;
                }
                prevMousePos = Input.mousePosition;

                VisualChipLocation(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            }
        }

        if (Input.GetKeyUp(KeyCode.Q) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.A)
         || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.Z) || Input.GetKeyUp(KeyCode.X))
        {
            var pressedChipIndexList = new List<int>();
            if (Input.GetKeyUp(KeyCode.Q))
            {
                pressedChipIndexList.Add(0);
            }
            if (Input.GetKeyUp(KeyCode.W))
            {
                pressedChipIndexList.Add(1);
            }
            if (Input.GetKeyUp(KeyCode.A))
            {
                pressedChipIndexList.Add(2);
            }
            if (Input.GetKeyUp(KeyCode.S))
            {
                pressedChipIndexList.Add(3);
            }
            if (Input.GetKeyUp(KeyCode.Z))
            {
                pressedChipIndexList.Add(4);
            }
            if (Input.GetKeyUp(KeyCode.X))
            {
                pressedChipIndexList.Add(5);
            }

            if (pressedChipIndexList.Contains(pressedChipIndex) && currentSelectedChip != null)
            {
                foreach (var frame in puzzleFrameList)
                {
                    frame.SetHighlight(false, Color.red);
                }

                var angle = dragImgRectTr.localEulerAngles;
                if (currentSelectedChip != null)
                {
                    dragImg.gameObject.SetActive(false);
                    bool success = false;

                    var offset = (new Vector2(currentSelectedChip.rowNum - 1, currentSelectedChip.colNum - 1) * chipSize) / 2;
                    ped.position = Input.mousePosition + new Vector3(-offset.x, offset.y, 0);
                    List<RaycastResult> results = new List<RaycastResult>();
                    es.RaycastAll(ped, results);
                    if (results.Count > 0)
                    {
                        bool isChipPanel = false;

                        foreach (var result in results)
                        {
                            if (result.gameObject.name.Contains("ChipPanel"))
                            {
                                isChipPanel = true;
                                break;
                            }
                        }

                        if (!isChipPanel)
                        {
                            PuzzleFrame frame = null;
                            foreach (var result in results)
                            {
                                frame = result.gameObject.GetComponent<PuzzleFrame>();
                                if (frame != null) break;
                            }

                            if (frame != null)
                            {
                                var fittableFrames = GetFittableFrameList(currPuzzle.frameDataTable, currentSelectedChip, frame.pfd.x, frame.pfd.y);
                                if (fittableFrames != null)
                                {
                                    var chipInstance = Instantiate(currentSelectedChip, chipPanelRectTr.transform);
                                    chipInstance.name = currentSelectedChip.name;
                                    for (int i = 0; i < fittableFrames.Count; i++)
                                    {
                                        fittableFrames[i].chip = chipInstance;
                                    }

                                    chipInstance.transform.parent = frame.transform;
                                    if (chipInstance.originRow.Length == currentSelectedChip.rowNum)
                                    {
                                        chipInstance.rectTr.sizeDelta = new Vector2(chipInstance.rowNum, chipInstance.colNum) * chipSize;
                                    }
                                    else
                                    {
                                        chipInstance.rectTr.sizeDelta = new Vector2(chipInstance.colNum, chipInstance.rowNum) * chipSize;
                                    }
                                    chipInstance.rectTr.anchoredPosition = Vector3.zero;

                                    chipInstance.rectTr.localEulerAngles = angle;
                                    var pos = chipInstance.rectTr.localPosition;
                                    if (angle.z < 1)
                                    {
                                    }
                                    else if (angle.z < 91)
                                    {
                                        pos.y -= dragImgRectTr.rect.height * (chipInstance.colNum * 1f / chipInstance.rowNum);
                                    }
                                    else if (angle.z < 181)
                                    {
                                        pos.y -= dragImgRectTr.rect.height;
                                        pos.x += dragImgRectTr.rect.width;
                                    }
                                    else if (angle.z < 271)
                                    {
                                        pos.x += dragImgRectTr.rect.width * (chipInstance.rowNum * 1f / chipInstance.colNum);
                                    }
                                    chipInstance.rectTr.localPosition = pos;

                                    if (!isFromPuzzle)
                                    {
                                        if (currentChipInPuzzleDic.ContainsKey(currentSelectedChipData))
                                        {
                                            currentChipInPuzzleDic[currentSelectedChipData] += 1;
                                        }
                                        else
                                        {
                                            currentChipInPuzzleDic.Add(currentSelectedChipData, 1);
                                        }
                                        currentSelectedChip.ResetColToOriginData();
                                        RefreshWeaponPowerData();
                                    }
                                    else
                                    {
                                        DestroyImmediate(currentSelectedChip.gameObject);
                                    }
                                    success = true;
                                }
                            }
                        }
                    }
                    if (!success)
                    {
                        currentSelectedChip.ResetColToOriginData();
                    }
                    currentSelectedChip = null;
                }
                pressedChipIndex = -1;
                prevMousePos = Vector3.zero;
            }
        }
    }

    public void StartPuzzle()
    {
        chipSize = 64;
        creditText.text = GameMgr.In.credit.ToString();
        SetOrderDatas();
        SetBluePrintDatas();
        SetPuzzle();
        SetChipDatas();
    }

    public void StartFeverModePuzzle()
    {
        chipSize = 64;
        creditText.text = GameMgr.In.credit.ToString();
        orderText.text = string.Empty;
        SetBluePrintDatas();
        SetPuzzle();
        SetChipDatas();
    }

    public void OnClickMakingDone()
    {
        if (isFeverMode)
        {
            MakingDone();
            return;
        }

        CommonTool.In.OpenConfirmPanel("제작을 완료하시겠습니까?", () =>
        {
            MakingDone();
        });
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A)
         || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.X))
        {
            return;
        }

        isFromPuzzle = false;
        currentSelectedChip = null;
        ped.position = eventData.pressPosition;
        List<RaycastResult> results = new List<RaycastResult>();
        es.RaycastAll(ped, results);
        if (results.Count > 0)
        {
            ChipObj targetChip = null;
            foreach (var result in results)
            {
                if (result.gameObject.name.Contains("PuzzleFrame"))
                {
                    isFromPuzzle = true;
                    targetChip = result.gameObject.GetComponent<PuzzleFrame>().pfd.chip;
                    break;
                }
            }

            if (targetChip == null)
            {
                targetChip = results[0].gameObject.GetComponent<ChipObj>();
            }

            if (targetChip == null)
            {
                if (results[0].gameObject.transform.childCount > 0)
                {
                    targetChip = results[0].gameObject.transform.GetChild(0).GetComponent<ChipObj>();
                }
            }

            if (targetChip != null)
            {
                if (targetChip.gameObject.activeInHierarchy)
                {
                    currentSelectedChipData = GameMgr.In.GetChip(targetChip.chipKey);
                    currentSelectedChip = targetChip;

                    currentSelectedChip.SaveCurrentRow();
                    if (!isFromPuzzle)
                    {
                        selectedChipName.text = currentSelectedChipData.chipName;
                        selectedChipPrice.text = "개당 " + currentSelectedChipData.price + " c";
                        selectedChipDesc.text = currentSelectedChipData.desc;
                    }
                    return;
                }
            }
        }
        isFromPuzzle = false;
        currentSelectedChipData = null;
        currentSelectedChip = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A)
         || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.X))
        {
            return;
        }

        if (currentSelectedChip != null)
        {
            dragImg.texture = currentSelectedChip.image.texture;
            if (currentSelectedChip.originRow.Length == currentSelectedChip.rowNum)
            {
                dragImgRectTr.sizeDelta = new Vector2(currentSelectedChip.rowNum, currentSelectedChip.colNum) * chipSize;
            }
            else
            {
                dragImgRectTr.sizeDelta = new Vector2(currentSelectedChip.colNum, currentSelectedChip.rowNum) * chipSize;
            }

            chipOffset = Vector3.zero;
            dragImgRectTr.localEulerAngles = currentSelectedChip.rectTr.localEulerAngles;
            var angle = dragImgRectTr.localEulerAngles;
            if (angle.z < 1)
            {
                chipOffset = new Vector3((-dragImgRectTr.sizeDelta.x) / 2, (dragImgRectTr.sizeDelta.y) / 2);
            }
            else if (angle.z < 91)
            {
                chipOffset = new Vector3((-dragImgRectTr.sizeDelta.x) / 2, (-dragImgRectTr.sizeDelta.y) / 2);
            }
            else if (angle.z < 181)
            {
                chipOffset = new Vector3((dragImgRectTr.sizeDelta.x) / 2, (-dragImgRectTr.sizeDelta.y) / 2);
            }
            else if (angle.z < 271)
            {
                chipOffset = new Vector3((dragImgRectTr.sizeDelta.x) / 2, (dragImgRectTr.sizeDelta.y) / 2);
            }
            dragImgRectTr.localPosition = Input.mousePosition + chipOffset - resolutionOffset;

            dragImg.gameObject.SetActive(true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A)
         || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.X))
        {
            return;
        }

        if (currentSelectedChip != null)
        {
            dragImgRectTr.localPosition += (Vector3)eventData.delta / canvas.scaleFactor;

            VisualChipLocation(eventData.position);
        }
    }

    private bool CanPlaceChip(PuzzleFrame frame)
    {
        var fittableFrames = GetFittableFrameList(currPuzzle.frameDataTable, currentSelectedChip, frame.pfd.x, frame.pfd.y);
        return fittableFrames != null;
    }

    public void VisualChipLocation(Vector2 chipCenter)
    {
        foreach (var frame in puzzleFrameList)
        {
            frame.SetHighlight(false, Color.red);
        }

        bool overallCanPlace = false;

        foreach (var frame in puzzleFrameList)
        {
            if (frame.pfd.patternNum == 1 && frame.pfd.chip == null)
            {
                if (IsChipWithinFrame(chipCenter, frame))
                {
                    overallCanPlace = CanPlaceChip(frame);
                    break;
                }
            }
        }

        foreach (var frame in puzzleFrameList)
        {
            if (frame.pfd.patternNum == 1 && frame.pfd.chip == null)
            {
                if (IsChipWithinFrame(chipCenter, frame))
                {
                    frame.SetHighlight(true, overallCanPlace ? Color.green : Color.red);
                }
            }
        }
    }

    private bool IsChipWithinFrame(Vector2 chipCenter, PuzzleFrame frame)
    {
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        chipCenter.x -= (currentSelectedChip.rowNum - 1) * chipSize / 2;
        chipCenter.y -= (currentSelectedChip.colNum - 1) * chipSize / 2;

        for (int i = 0; i < currentSelectedChip.rowNum; i++)
        {
            for (int j = 0; j < currentSelectedChip.colNum; j++)
            {
                Vector2 checkPosition = new Vector2(
                    chipCenter.x + i * chipSize,
                    chipCenter.y + j * chipSize
                );

                if (RectTransformUtility.RectangleContainsScreenPoint(frameRect, checkPosition, canvas.worldCamera))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A)
         || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.X))
        {
            return;
        }

        foreach (var frame in puzzleFrameList)
        {
            frame.SetHighlight(false, Color.red);
        }

        var angle = dragImgRectTr.localEulerAngles;
        if (currentSelectedChip != null)
        {
            dragImg.gameObject.SetActive(false);
            bool success = false;

            var offset = (new Vector2(currentSelectedChip.rowNum - 1, currentSelectedChip.colNum - 1) * chipSize) / 2;
            ped.position = Input.mousePosition + new Vector3(-offset.x, offset.y, 0);
            List<RaycastResult> results = new List<RaycastResult>();
            es.RaycastAll(ped, results);
            if (results.Count > 0)
            {
                bool isChipPanel = false;

                foreach (var result in results)
                {
                    if (result.gameObject.name.Contains("ChipPanel"))
                    {
                        isChipPanel = true;
                        break;
                    }
                }

                if (isChipPanel)
                {
                    if (isFromPuzzle)
                    {
                        DestroyImmediate(currentSelectedChip.gameObject);

                        if (currentChipInPuzzleDic[currentSelectedChipData] == 1)
                        {
                            currentChipInPuzzleDic.Remove(currentSelectedChipData);
                        }
                        else
                        {
                            currentChipInPuzzleDic[currentSelectedChipData] -= 1;
                        }
                        RefreshWeaponPowerData();

                        if (isTutorial)
                        {
                            makingDone.gameObject.SetActive(IsWeaponRequiredAbilitySatisfied());
                        }

                        success = true;
                    }
                }
                else
                {
                    PuzzleFrame frame = null;
                    foreach (var result in results)
                    {
                        frame = result.gameObject.GetComponent<PuzzleFrame>();
                        if (frame != null) break;
                    }

                    if (frame != null)
                    {
                        var fittableFrames = GetFittableFrameList(currPuzzle.frameDataTable, currentSelectedChip, frame.pfd.x, frame.pfd.y);
                        if (fittableFrames != null)
                        {
                            var chipInstance = Instantiate(currentSelectedChip, chipPanelRectTr.transform);
                            chipInstance.name = currentSelectedChip.name;
                            for (int i = 0; i < fittableFrames.Count; i++)
                            {
                                fittableFrames[i].chip = chipInstance;
                            }

                            chipInstance.transform.parent = frame.transform;
                            if (chipInstance.originRow.Length == currentSelectedChip.rowNum)
                            {
                                chipInstance.rectTr.sizeDelta = new Vector2(chipInstance.rowNum, chipInstance.colNum) * chipSize;
                            }
                            else
                            {
                                chipInstance.rectTr.sizeDelta = new Vector2(chipInstance.colNum, chipInstance.rowNum) * chipSize;
                            }
                            chipInstance.rectTr.anchoredPosition = Vector3.zero;

                            chipInstance.rectTr.localEulerAngles = angle;
                            var pos = chipInstance.rectTr.localPosition;
                            if (angle.z < 1)
                            {
                            }
                            else if (angle.z < 91)
                            {
                                pos.y -= dragImgRectTr.rect.height * (chipInstance.colNum * 1f / chipInstance.rowNum);
                            }
                            else if (angle.z < 181)
                            {
                                pos.y -= dragImgRectTr.rect.height;
                                pos.x += dragImgRectTr.rect.width;
                            }
                            else if (angle.z < 271)
                            {
                                pos.x += dragImgRectTr.rect.width * (chipInstance.rowNum * 1f / chipInstance.colNum);
                            }
                            chipInstance.rectTr.localPosition = pos;

                            if (!isFromPuzzle)
                            {
                                if (currentChipInPuzzleDic.ContainsKey(currentSelectedChipData))
                                {
                                    currentChipInPuzzleDic[currentSelectedChipData] += 1;
                                }
                                else
                                {
                                    currentChipInPuzzleDic.Add(currentSelectedChipData, 1);
                                }
                                currentSelectedChip.ResetColToOriginData();
                                RefreshWeaponPowerData();
                            }
                            else
                            {
                                DestroyImmediate(currentSelectedChip.gameObject);
                            }

                            if (isTutorial)
                            {
                                makingDone.gameObject.SetActive(IsWeaponRequiredAbilitySatisfied());
                            }

                            success = true;
                        }
                    }
                }
            }
            if (!success)
            {
                if (isFromPuzzle)
                {
                    currentSelectedChip.ResetColToCurrentData();
                }
                else
                {
                    currentSelectedChip.ResetColToOriginData();
                }
            }
            currentSelectedChip = null;
        }
        isFromPuzzle = false;
    }

    private void SetPuzzle()
    {
        currPuzzle = GetPuzzle();
        InstantiateFrames(currPuzzle.frameDataTable);
        enabledFrameCnt = 0;
        foreach (var frameData in currPuzzle.frameDataTable)
        {
            if (frameData.patternNum == 1) enabledFrameCnt++;
        }
    }

    private Puzzle GetPuzzle()
    {
        Puzzle puzzle = new Puzzle();
        var lines = GameMgr.In.currentBluePrint.puzzleCsv.text.Split('\n');
        var lineList = new List<string>();
        foreach (var line in lines)
        {
            var trimLine = line.Trim();
            if (!string.IsNullOrEmpty(trimLine))
            {
                lineList.Add(trimLine);
            }
        }
        puzzle.y = lineList.Count;
        puzzle.x = lineList[0].Split(',').Length;
        puzzle.frameDataTable = new PuzzleFrameData[puzzle.y, puzzle.x];
        for (int i = 0; i < lineList.Count; i++)
        {
            var elements = lineList[i].Split(',');
            if (i == 0) puzzle.x = elements.Length;

            for (int j = 0; j < elements.Length; j++)
            {
                int targetNum = 0;
                if (!Int32.TryParse(elements[j], out targetNum))
                {
                    Debug.Log("퍼즐조각정보 로드 실패. puzzle" + GameMgr.In.currentBluePrint.puzzleCsv.text + ": " + i + ", " + j);
                    return null;
                }
                puzzle.frameDataTable[i, j] = new PuzzleFrameData(targetNum, j, i);
            }
        }

        return puzzle;
    }

    private void InstantiateFrames(PuzzleFrameData[,] frameDataTable)
    {
        foreach (var frameData in frameDataTable)
        {
            var frame = Instantiate(puzzleFrame, puzzleGrid.transform);
            frame.SetPuzzleFrameData(frameData);
            puzzleFrameList.Add(frame);
            frame.image.texture = textureList[frameData.patternNum];
        }
    }

    private List<PuzzleFrameData> GetFittableFrameList(PuzzleFrameData[,] targetTable, ChipObj chip, int x, int y)
    {
        List<PuzzleFrameData> pfdList = new List<PuzzleFrameData>();
        var table = chip.row;
        for (int i = 0; i < table.Length; i++)
        {
            for (int j = 0; j < table[i].col.Length; j++)
            {
                if (!table[i].col[j])
                {
                    continue;
                }

                if (targetTable.GetLength(0) <= y + j) return null;
                if (targetTable.GetLength(1) <= x + i) return null;

                if (targetTable[y + j, x + i].patternNum == 0)
                {
                    return null;
                }

                if (x + i >= currPuzzle.x || y + j >= currPuzzle.y)
                {
                    return null;
                }

                if (targetTable[y + j, x + i].chip != null && !targetTable[y + j, x + i].chip.Equals(chip))
                {
                    return null;
                }

                pfdList.Add(targetTable[y + j, x + i]);
            }
        }
        return pfdList;
    }

    private void SetOrderDatas()
    {
        var order = GameMgr.In.currentOrder;
        this.orderText.text = mgr2.mainChatText.text.Replace(" ▼", "").Replace("\n", " ");
    }

    private void SetBluePrintDatas()
    {
        var bp = GameMgr.In.currentBluePrint;
        bluePrintName.text = bp.name;

        StringBuilder sb = new StringBuilder();
        foreach (var requiredAbility in bp.requiredChipAbilityList)
        {
            var ability = GameMgr.In.GetAbility(requiredAbility.abilityKey);
            sb.Append(ability.name).Append("+").Append(requiredAbility.count).Append(" ");
        }
        sb.Length--;
        requiredAbilityText.text = sb.ToString();
    }

    private void SetChipDatas()
    {
        if (creatableChipKeyList != null && creatableChipKeyList.Count > 0) return;

        var creatableChips = GameMgr.In.chipTable.chipList.FindAll(x => x.createEnable);
        creatableChipKeyList = new List<string>();
        foreach (var cc in creatableChips)
        {
            creatableChipKeyList.Add(cc.chipKey);
        }
        foreach (var chip in chipList)
        {
            chip.SaveOriginRow();
            chip.backgroundSC.gameObject.SetActive(creatableChipKeyList.Contains(chip.chipKey));
        }
        SortChipList();
    }

    private void OnClickSortTarget()
    {
        isAscending = !isAscending;

        if (isAscending)
        {
            sortOrderSC.SetOnSprite();
        }
        else
        {
            sortOrderSC.SetOffSprite();
        }

        SortChipList();
    }

    private void SortChipList()
    {
        var chipList = GameMgr.In.chipTable.chipList;
        var orderedChipKeyList = new List<string>();
        if (isAscending)
        {
            orderedChipKeyList = chipList.OrderBy(x => x.price).ThenBy(x => x.chipKey).Select(x => x.chipKey).ToList();
        }
        else
        {
            orderedChipKeyList = chipList.OrderByDescending(x => x.price).ThenByDescending(x => x.chipKey).Select(x => x.chipKey).ToList();
        }

        for (int i = 0; i < orderedChipKeyList.Count; i++)
        {
            var matchedChip = this.chipList.Find(x => x.chipKey.Equals(orderedChipKeyList[i]));
            if (matchedChip == null) continue;

            matchedChip.backgroundSC.transform.SetSiblingIndex(i);
        }
    }

    private void RefreshWeaponPowerData()
    {
        StringBuilder currAbilitySB = new StringBuilder();
        StringBuilder usedChipSB = new StringBuilder();

        currentAbilityInPuzzleDic.Clear();
        foreach (var chip in currentChipInPuzzleDic.Keys)
        {
            usedChipSB.Append(chip.chipName).Append(" 칩셋 ").Append(currentChipInPuzzleDic[chip]).Append("개\n");

            foreach (var ability in chip.abilityList)
            {
                var targetAbility = GameMgr.In.GetAbility(ability.abilityKey);
                if (currentAbilityInPuzzleDic.ContainsKey(targetAbility))
                {
                    currentAbilityInPuzzleDic[targetAbility] += ability.count * currentChipInPuzzleDic[chip];
                }
                else
                {
                    currentAbilityInPuzzleDic.Add(targetAbility, ability.count * currentChipInPuzzleDic[chip]);
                }
            }
        }

        foreach (var ability in currentAbilityInPuzzleDic.Keys)
        {
            currAbilitySB.Append(ability.name).Append("+").Append(currentAbilityInPuzzleDic[ability]).Append(" ");
        }

        if (currAbilitySB.Length > 0)
        {
            currAbilitySB.Length--;
        }
        if (usedChipSB.Length > 0)
        {
            usedChipSB.Length--;
        }

        currentAbilityText.text = currAbilitySB.ToString();
        usedChipText.text = usedChipSB.ToString();
    }

    private void OnClickRevertChips()
    {
        foreach (var frame in puzzleFrameList)
        {
            foreach (Transform tr in frame.transform)
            {
                DestroyImmediate(tr.gameObject);
            }
        }
        if (makingDone != null && isTutorial)
        {
            makingDone.gameObject.SetActive(false);
        }
        currentChipInPuzzleDic.Clear();
        currentAbilityInPuzzleDic.Clear();
        RefreshWeaponPowerData();
    }

    private void MakingDone()
    {
        int score = 0;
        if (IsWeaponRequiredAbilitySatisfied()) score++;
        if (IsOrderRequirementSatisfied()) score++;
        if (score == 2)
        {
            if (isPerfectState()) score++;
        }

        foreach (var obj in puzzleFrameList)
        {
            Destroy(obj.gameObject);
        }
        puzzleFrameList.Clear();

        currentChipInPuzzleDic.Clear();
        currentAbilityInPuzzleDic.Clear();
        chipObjDic.Clear();
        currentAbilityText.text = string.Empty;
        usedChipText.text = string.Empty;

        switch (score)
        {
            case 0:
                GameMgr.In.dayFame -= 25;
                GameMgr.In.fame -= 25;
                if (!isFeverMode)
                {
                    mgr2.orderState = GameSceneMgr.OrderState.Failed;
                }
                break;
            case 1:
                GameMgr.In.dayFame -= 10;
                GameMgr.In.fame -= 10;
                if (!isFeverMode)
                {
                    mgr2.orderState = GameSceneMgr.OrderState.Failed;
                }
                break;
            case 2:
                if (!isTutorial)
                {
                    int sellPrice1 = GameMgr.In.currentBluePrint.sellPrice;
                    int bonus1 = GetBonusCredit(sellPrice1);
                    GameMgr.In.credit += sellPrice1 + bonus1;
                    if (!isFeverMode)
                    {
                        mgr2.goldText.text = GameMgr.In.credit.ToString();
                    }
                    GameMgr.In.dayRevenue += sellPrice1;
                    GameMgr.In.dayBonusRevenue += bonus1;
                }
                GameMgr.In.dayFame += 10;
                GameMgr.In.fame += 10;
                if (!isFeverMode)
                {
                    mgr2.orderState = GameSceneMgr.OrderState.Succeed;
                }
                break;
            case 3:
                int sellPrice2 = GameMgr.In.currentBluePrint.sellPrice;
                int bonus2 = GetBonusCredit(sellPrice2, 0.1f);
                GameMgr.In.credit += sellPrice2 + bonus2;
                if (!isFeverMode)
                {
                    mgr2.goldText.text = GameMgr.In.credit.ToString();
                }
                GameMgr.In.dayRevenue += sellPrice2;
                GameMgr.In.dayBonusRevenue += bonus2;
                GameMgr.In.dayFame += 25;
                GameMgr.In.fame += 25;
                if (!isFeverMode)
                {
                    mgr2.orderState = GameSceneMgr.OrderState.Succeed;
                }
                break;
        }

        if (isTutorial)
        {
            isTutorial = false;
        }

        if (OnMakingDone != null)
        {
            OnMakingDone.Invoke(score);
            OnMakingDone = null;
        }
    }

    private bool IsWeaponRequiredAbilitySatisfied()
    {
        foreach (var requiredAbility in GameMgr.In.currentBluePrint.requiredChipAbilityList)
        {
            var targetAbility = currentAbilityInPuzzleDic.FirstOrDefault(x => x.Key.abilityKey.Equals(requiredAbility.abilityKey));
            if (targetAbility.Equals(default(KeyValuePair<Ability, int>)))
            {
                Debug.Log("무기 필수조건 불충족");
                return false;
            }

            if (targetAbility.Value < requiredAbility.count)
            {
                Debug.Log("무기 필수조건 불충족");
                return false;
            }
        }
        return true;
    }

    private bool IsOrderRequirementSatisfied()
    {
        // check blueprint
        if (GameMgr.In.currentOrder.requiredBlueprintKeyList.Count > 0)
        {
            if (!GameMgr.In.currentOrder.requiredBlueprintKeyList.Contains(GameMgr.In.currentBluePrint.bluePrintKey))
            {
                Debug.Log("청사진 요구조건 불충족");
                return false;
            }
        }

        // check ability
        foreach (var requiredAbility in GameMgr.In.currentOrder.requiredAbilityList)
        {
            var targetAbility = currentAbilityInPuzzleDic.FirstOrDefault(x => x.Key.abilityKey.Equals(requiredAbility.abilityKey));
            if (targetAbility.Equals(default(KeyValuePair<Ability, int>)))
            {
                Debug.Log("능력치 요구조건 불충족");
                return false;
            }

            if (targetAbility.Value < requiredAbility.count)
            {
                Debug.Log("능력치 요구조건 불충족");
                return false;
            }
        }

        // check condition
        foreach (var pair in currentChipInPuzzleDic)
        {
            var chipObj = chipList.Find(x => x.chipKey.Equals(pair.Key.chipKey));
            chipObjDic.Add(chipObj, pair.Value);
        }
        if (!GameMgr.In.orderTable.IsConditionMatched(enabledFrameCnt, chipObjDic, currentAbilityInPuzzleDic, GameMgr.In.currentOrder.condition))
        {
            Debug.Log("상태 요구조건 불충족");
            return false;
        }

        // check gimmick
        if (!GameMgr.In.orderTable.IsGimmickMatched(puzzleFrameList))
        {
            Debug.Log("특수요구조건 불충족");
            return false;
        }

        return true;
    }

    private bool isPerfectState()
    {
        bool isPerfect = GameMgr.In.orderTable.GetTotalSizeOfChips(chipObjDic) == enabledFrameCnt;
        if (!isPerfect) Debug.Log("완벽한 상태가 아님");
        return isPerfect;
    }

    private int GetBonusCredit(int sellPrice, float additionalBonus = -1f)
    {
        int bonus = 0;

        if (additionalBonus != -1f)
        {
            bonus += Mathf.FloorToInt(sellPrice * additionalBonus);
        }

        if (GameMgr.In.fame >= 801)
        {
            bonus += Mathf.FloorToInt(sellPrice * 0.1f);
        }
        else if (GameMgr.In.fame >= 601)
        {
            bonus += Mathf.FloorToInt(sellPrice * 0.06f);
        }
        else if (GameMgr.In.fame >= 401)
        {
            bonus += Mathf.FloorToInt(sellPrice * 0.04f);
        }
        else if (GameMgr.In.fame >= 201)
        {
            bonus += Mathf.FloorToInt(sellPrice * 0.02f);
        }

        return bonus;
    }

    [ContextMenu("LogPuzzleFrameDatas")]
    private void LogPuzzleFrameDatas()
    {
        StringBuilder sb2 = new StringBuilder();
        int i = 0;
        foreach (var frame in currPuzzle.frameDataTable)
        {
            if (frame != null && frame.chip != null)
            {
                sb2.Append(frame.chip.chipKey).Append(", ");
            }
            else
            {
                sb2.Append("0, ");
            }
            i++;
            if (i == 8)
            {
                sb2.Append("\n");
                i = 0;
            }
        }
        Debug.Log(sb2.ToString());
    }

}

