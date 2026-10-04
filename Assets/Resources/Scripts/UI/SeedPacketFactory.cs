using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class SeedPacketView : MonoBehaviour
{
    Image frame; GameObject mark;
    public void Configure(Image value, GameObject selectedMark) { frame=value; mark=selectedMark; SetSelected(false); }
    public void SetSelected(bool selected)
    {
        if(mark!=null) mark.SetActive(selected);
        if(frame!=null) frame.color=selected ? new Color(.98f,.82f,.28f,1) : new Color(.72f,.62f,.39f,1);
    }
}

public static class SeedPacketFactory
{
    public static readonly Vector2 GameplaySize=new Vector2(30,43);
    public static readonly Vector2 ChoiceSize=new Vector2(86,108);

    public static SeedPacketView CreateChoice(PlantLoadoutEntry entry, Transform parent, Vector2 size, UnityAction action)
    {
        GameObject root=CreateBase(entry,parent,size,true);
        root.GetComponent<Button>().onClick.AddListener(action);
        Image mark=ImageObject("Selected",root.transform,new Color(.22f,.72f,.16f,.34f)); Stretch(mark.rectTransform,.035f); mark.raycastTarget=false;
        Text tick=TextObject("Tick",mark.transform,"✓",Mathf.RoundToInt(size.y*.3f),TextAnchor.UpperRight,Color.white); Anchor(tick.rectTransform,.55f,.55f,.94f,.96f); tick.raycastTarget=false;
        SeedPacketView view=root.AddComponent<SeedPacketView>(); view.Configure(root.GetComponent<Image>(),mark.gameObject); return view;
    }

    public static SeedPacketView CreateSelectionCard(PlantLoadoutEntry entry, Transform parent,
        float targetHeight, UnityAction action)
    {
        Sprite artwork = LoadCardArtwork(entry);
        if (artwork == null)
            return CreateChoice(entry, parent, new Vector2(targetHeight * .72f, targetHeight), action);

        float aspect = artwork.rect.height > 0f ? artwork.rect.width / artwork.rect.height : .72f;
        Vector2 size = new Vector2(Mathf.Clamp(targetHeight * aspect, targetHeight * .62f,
            targetHeight * .90f), targetHeight);
        GameObject root = new GameObject(entry.Key + " Selection Card", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        root.GetComponent<RectTransform>().sizeDelta = size;

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = size.x;
        layout.minHeight = layout.preferredHeight = size.y;
        layout.flexibleWidth = layout.flexibleHeight = 0f;

        Image image = root.GetComponent<Image>();
        image.sprite = artwork;
        image.color = Color.white;
        image.preserveAspect = true;
        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Image mark = ImageObject("Selected", root.transform, new Color(.28f, .90f, .16f, .30f));
        Stretch(mark.rectTransform, .025f);
        mark.raycastTarget = false;
        Text tick = TextObject("Tick", mark.transform, "✓", Mathf.RoundToInt(targetHeight * .25f),
            TextAnchor.UpperRight, Color.white);
        Anchor(tick.rectTransform, .53f, .62f, .94f, .97f);
        tick.raycastTarget = false;

        SeedPacketView view = root.AddComponent<SeedPacketView>();
        // Không đổi màu ảnh thẻ gốc khi chọn; chỉ bật lớp đánh dấu phía trên.
        view.Configure(null, mark.gameObject);
        return view;
    }

    public static Card CreateGameplayCard(PlantLoadoutEntry entry, Transform parent)
    {
        Sprite artwork = LoadCardArtwork(entry);
        GameObject root = artwork != null
            ? CreateGameplayArtworkCard(entry, parent, artwork)
            : CreateBase(entry, parent, GameplaySize, false);
        root.name=entry.Key+"Card";
        Button button=root.GetComponent<Button>();
        AudioSource audio=root.AddComponent<AudioSource>(); audio.playOnAwake=false; audio.clip=Resources.Load<AudioClip>("Sounds/UI/SeedAndShovelBank/seedlift");
        // Availability/cooldown only covers the artwork. The sun price must always remain readable.
        Image unavailable=ImageObject("Unavailable",root.transform,new Color(0,0,0,.58f)); Anchor(unavailable.rectTransform,.025f,.22f,.975f,.975f); unavailable.raycastTarget=false;
        Image cooldown=ImageObject("Cooldown",root.transform,new Color(0,0,0,.68f)); Anchor(cooldown.rectTransform,.025f,.22f,.975f,.975f); cooldown.type=Image.Type.Filled; cooldown.fillMethod=Image.FillMethod.Vertical; cooldown.fillOrigin=(int)Image.OriginVertical.Top; cooldown.fillAmount=1; cooldown.raycastTarget=false;
        Card card=root.AddComponent<Card>(); card.upperImageObj=unavailable.gameObject; card.lowerImage=cooldown; card.lowerImageObj=cooldown.gameObject; card.myButton=button; card.coolingTime=entry.Cooldown; card.plantName=entry.PlantName; card.sunNeeded=entry.Cost;
        Transform costLayer=root.transform.Find("Cost Background"); if(costLayer!=null) costLayer.SetAsLastSibling();
        button.onClick.AddListener(card.click); return card;
    }

    private static GameObject CreateGameplayArtworkCard(PlantLoadoutEntry entry, Transform parent, Sprite artwork)
    {
        GameObject root = new GameObject(entry.Key + " Gameplay Card", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        root.GetComponent<RectTransform>().sizeDelta = GameplaySize;
        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = GameplaySize.x;
        layout.minHeight = layout.preferredHeight = GameplaySize.y;
        layout.flexibleWidth = layout.flexibleHeight = 0f;
        Image image = root.GetComponent<Image>();
        image.sprite = artwork;
        image.color = Color.white;
        image.preserveAspect = true;
        root.GetComponent<Button>().targetGraphic = image;
        return root;
    }

    static GameObject CreateBase(PlantLoadoutEntry entry, Transform parent, Vector2 size, bool showName)
    {
        GameObject root=new GameObject(entry.Key+" Seed Packet",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button),typeof(LayoutElement)); root.transform.SetParent(parent,false);
        root.GetComponent<RectTransform>().sizeDelta=size;
        LayoutElement layout=root.GetComponent<LayoutElement>(); layout.minWidth=layout.preferredWidth=size.x; layout.minHeight=layout.preferredHeight=size.y; layout.flexibleWidth=layout.flexibleHeight=0;
        Image frame=root.GetComponent<Image>(); frame.color=new Color(.72f,.62f,.39f,1); Button button=root.GetComponent<Button>(); button.targetGraphic=frame;
        Image inner=ImageObject("Inner",root.transform,new Color(.20f,.27f,.12f,1)); Anchor(inner.rectTransform,.045f,.045f,.955f,.955f); inner.raycastTarget=false;
        Image iconBack=ImageObject("Icon Background",root.transform,new Color(.42f,.51f,.24f,1)); Anchor(iconBack.rectTransform,.09f,.22f,.91f,showName?.78f:.88f); iconBack.raycastTarget=false;
        iconBack.gameObject.AddComponent<RectMask2D>();
        Image icon=ImageObject("Plant Icon",iconBack.transform,Color.white); icon.sprite=LoadIcon(entry); icon.preserveAspect=true;
        bool imported=ImportedPlantRuntime.Supports(entry.Key);
        Anchor(icon.rectTransform,imported?-.12f:.04f,imported?-.20f:.04f,imported?1.12f:.96f,imported?1.18f:.96f); icon.raycastTarget=false;
        if(showName) { Text label=TextObject("Plant Name",root.transform,entry.DisplayName,Mathf.Max(9,Mathf.RoundToInt(size.y*.115f)),TextAnchor.MiddleCenter,new Color(.98f,.95f,.78f,1)); Anchor(label.rectTransform,.07f,.79f,.93f,.94f); label.raycastTarget=false; }
        Image costBack=ImageObject("Cost Background",root.transform,new Color(.06f,.08f,.03f,1)); Anchor(costBack.rectTransform,.06f,.035f,.94f,.235f); costBack.raycastTarget=false;
        Text cost=TextObject("Sun Cost",costBack.transform,entry.Cost.ToString(),Mathf.Max(11,Mathf.RoundToInt(size.y*.17f)),TextAnchor.MiddleCenter,new Color(1,.92f,.18f,1)); Stretch(cost.rectTransform); cost.fontStyle=FontStyle.Bold; cost.raycastTarget=false;
        Outline costOutline=cost.gameObject.AddComponent<Outline>(); costOutline.effectColor=new Color(0,0,0,.9f); costOutline.effectDistance=new Vector2(1,-1);
        return root;
    }

    public static Sprite LoadIcon(PlantLoadoutEntry entry) { Sprite sprite=string.IsNullOrEmpty(entry.IconPath)?null:Resources.Load<Sprite>(entry.IconPath); if(sprite!=null) return sprite; return ImportedPlantRuntime.CardPreview(entry.Key)??ImportedPlantRuntime.Preview(entry.Key); }
    public static Sprite LoadCardArtwork(PlantLoadoutEntry entry)
    {
        Sprite artwork = Resources.Load<Sprite>("Sprites/UI/Card/" + entry.Key + "Slot");
        return artwork != null ? artwork : ImportedPlantRuntime.CardPreview(entry.Key);
    }
    static Image ImageObject(string name,Transform parent,Color color) { GameObject go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)); go.transform.SetParent(parent,false); Image image=go.GetComponent<Image>(); image.color=color; return image; }
    static Text TextObject(string name,Transform parent,string value,int size,TextAnchor alignment,Color color) { GameObject go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)); go.transform.SetParent(parent,false); Text text=go.GetComponent<Text>(); text.font=Resources.Load<Font>("Fonts/Baloo2")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text=value; text.fontSize=size; text.resizeTextForBestFit=true; text.resizeTextMinSize=Mathf.Min(7,size); text.resizeTextMaxSize=size; text.alignment=alignment; text.color=color; return text; }
    static void Stretch(RectTransform rect,float margin=0) { Anchor(rect,margin,margin,1-margin,1-margin); }
    static void Anchor(RectTransform rect,float x0,float y0,float x1,float y1) { rect.anchorMin=new Vector2(x0,y0); rect.anchorMax=new Vector2(x1,y1); rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero; }
}
