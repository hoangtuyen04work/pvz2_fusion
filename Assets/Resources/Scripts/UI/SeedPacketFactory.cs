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
    public static readonly Vector2 GameplaySize=new Vector2(46,64);
    public static readonly Vector2 ChoiceSize=new Vector2(86,108);

    public static SeedPacketView CreateChoice(PlantLoadoutEntry entry, Transform parent, Vector2 size, UnityAction action)
    {
        GameObject root=CreateBase(entry,parent,size,true);
        root.GetComponent<Button>().onClick.AddListener(action);
        Image mark=ImageObject("Selected",root.transform,new Color(.22f,.72f,.16f,.34f)); Stretch(mark.rectTransform,.035f); mark.raycastTarget=false;
        Text tick=TextObject("Tick",mark.transform,"✓",Mathf.RoundToInt(size.y*.3f),TextAnchor.UpperRight,Color.white); Anchor(tick.rectTransform,.55f,.55f,.94f,.96f); tick.raycastTarget=false;
        SeedPacketView view=root.AddComponent<SeedPacketView>(); view.Configure(root.GetComponent<Image>(),mark.gameObject); return view;
    }

    public static Card CreateGameplayCard(PlantLoadoutEntry entry, Transform parent)
    {
        GameObject root=CreateBase(entry,parent,GameplaySize,false); root.name=entry.Key+"Card";
        Button button=root.GetComponent<Button>();
        AudioSource audio=root.AddComponent<AudioSource>(); audio.playOnAwake=false; audio.clip=Resources.Load<AudioClip>("Sounds/UI/SeedAndShovelBank/seedlift");
        // Availability/cooldown only covers the artwork. The sun price must always remain readable.
        Image unavailable=ImageObject("Unavailable",root.transform,new Color(0,0,0,.58f)); Anchor(unavailable.rectTransform,.025f,.22f,.975f,.975f); unavailable.raycastTarget=false;
        Image cooldown=ImageObject("Cooldown",root.transform,new Color(0,0,0,.68f)); Anchor(cooldown.rectTransform,.025f,.22f,.975f,.975f); cooldown.type=Image.Type.Filled; cooldown.fillMethod=Image.FillMethod.Vertical; cooldown.fillOrigin=(int)Image.OriginVertical.Top; cooldown.fillAmount=1; cooldown.raycastTarget=false;
        Card card=root.AddComponent<Card>(); card.upperImageObj=unavailable.gameObject; card.lowerImage=cooldown; card.lowerImageObj=cooldown.gameObject; card.myButton=button; card.coolingTime=entry.Cooldown; card.plantName=entry.PlantName; card.sunNeeded=entry.Cost;
        Transform costLayer=root.transform.Find("Cost Background"); if(costLayer!=null) costLayer.SetAsLastSibling();
        button.onClick.AddListener(card.click); return card;
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
    static Image ImageObject(string name,Transform parent,Color color) { GameObject go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)); go.transform.SetParent(parent,false); Image image=go.GetComponent<Image>(); image.color=color; return image; }
    static Text TextObject(string name,Transform parent,string value,int size,TextAnchor alignment,Color color) { GameObject go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)); go.transform.SetParent(parent,false); Text text=go.GetComponent<Text>(); text.font=Resources.Load<Font>("Fonts/Baloo2")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text=value; text.fontSize=size; text.resizeTextForBestFit=true; text.resizeTextMinSize=Mathf.Min(7,size); text.resizeTextMaxSize=size; text.alignment=alignment; text.color=color; return text; }
    static void Stretch(RectTransform rect,float margin=0) { Anchor(rect,margin,margin,1-margin,1-margin); }
    static void Anchor(RectTransform rect,float x0,float y0,float x1,float y1) { rect.anchorMin=new Vector2(x0,y0); rect.anchorMax=new Vector2(x1,y1); rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero; }
}
