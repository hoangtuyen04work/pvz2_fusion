using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public static class OriginalPvZPlantRuntime
{
    private const string Root = "Sprites/Imported/OriginalPvZ/Pults/";
    private static readonly string[] Keys = { "CabbagePult", "KernelPult", "MelonPult", "UmbrellaLeaf" };

    public static bool Supports(string key) => Keys.Any(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));

    private static string SourceName(string key) => key.Equals("KernelPult", StringComparison.OrdinalIgnoreCase) ? "Cornpult" :
        key.Equals("UmbrellaLeaf", StringComparison.OrdinalIgnoreCase) ? "Umbrellaleaf" : key;

    private static string ClipPath(string key, string clip) => Root + "Rendered/" + SourceName(key) + "/" + clip;

    public static Sprite Preview(string key)
    {
        return Resources.LoadAll<Sprite>(ClipPath(key, "Idle"))
            .OrderBy(s => ImportedPlantRuntime.NaturalIndex(s.name)).FirstOrDefault();
    }

    public static GameObject Create(string key, Vector3 position, Transform parent)
    {
        GameObject go = new GameObject(key) { tag = "Plant" };
        go.transform.SetParent(parent, false); go.transform.position = position;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        go.AddComponent<BoxCollider2D>().size = new Vector2(.66f, .76f);
        go.AddComponent<AudioSource>().playOnAwake = false;
        go.AddComponent<Animator>();
        GameObject halo = new GameObject("Halo"); halo.transform.SetParent(go.transform, false); halo.SetActive(false);
        RuntimeFrameAnimator view = go.AddComponent<RuntimeFrameAnimator>();
        view.Configure(renderer, ClipPath(key, "Idle"), 12f);
        OriginalPvZPlant plant = go.AddComponent<OriginalPvZPlant>(); plant.Configure(key, view);
        return go;
    }
}

public sealed class OriginalPvZPlant : Plant
{
    private const string Root="Sprites/Imported/OriginalPvZ/Pults/Rendered/";
    private string key; private RuntimeFrameAnimator view; private float nextShot; private float rate=1f;
    private string SourceName=>key=="KernelPult"?"Cornpult":key=="UmbrellaLeaf"?"Umbrellaleaf":key;
    private string Clip(string name)=>Root+SourceName+"/"+name;
    public void Configure(string value, RuntimeFrameAnimator plantView){key=value;view=plantView;bloodVolume=key=="UmbrellaLeaf"?400:300;}
    protected override void Start(){base.Start();nextShot=Time.time+.8f;}
    private void Update()
    {
        if(key=="UmbrellaLeaf") return;
        if(Time.time<nextShot)return;Zombie target=FindObjectsByType<Zombie>().Where(z=>!z.IsHypnotized&&z.bloodVolume>0&&z.pos_row==row&&z.transform.position.x>transform.position.x).OrderBy(z=>z.transform.position.x).FirstOrDefault();
        if(target==null){nextShot=Time.time+.2f;return;}StartCoroutine(Shoot(target));nextShot=Time.time+2.9f/rate;
    }
    private IEnumerator Shoot(Zombie target)
    {
        bool butter = key=="KernelPult" && UnityEngine.Random.value<.25f;
        string shootingClip = key=="KernelPult" ? (butter ? "ShootingButter" : "ShootingKernel") : "Shooting";
        float baseAnimationSpeed = key=="KernelPult" ? 1.03f : 1.15f;
        int releaseFrame = key=="CabbagePult" ? 12 : key=="KernelPult" ? 11 : 13;
        float playbackSpeed = baseAnimationSpeed*rate;
        view.PlayOnce(Clip(shootingClip),playbackSpeed,()=>view.SetFrames(Clip("Idle")));
        yield return new WaitForSeconds(releaseFrame/(12f*playbackSpeed));
        if(target!=null)OriginalPultProjectile.Create(key,transform.position+LaunchOffset(),target,butter,row);
    }
    private Vector3 LaunchOffset(){return key=="CabbagePult"?new Vector3(.062f,.347f,0):key=="KernelPult"?new Vector3(.273f,.434f,0):new Vector3(.36f,.459f,0);}
    public void PlayUmbrellaBlock(){if(key=="UmbrellaLeaf")view.PlayOnce(Clip("Block"),()=>view.SetFrames(Clip("Idle")));}
    public bool IsUmbrellaSupport=>key=="UmbrellaLeaf"&&bloodVolume>0;
    protected override void intensify_specific(){rate=1.5f;}
    protected override void cancelIntensify_specific(){rate=1f;}
}

public sealed class OriginalPultProjectile : MonoBehaviour
{
    private const float ButterStunDuration = 5f;
    private const float ResonanceDamageMultiplier = 1.5f;
    private static Sprite[] melonPieces;
    private Zombie target;private OriginalPvZPlant umbrella;private SpriteRenderer projectileRenderer;
    private Vector3 from,to;private float elapsed,duration,arcHeight;private int damage,row;private bool butter,melon,routingToUmbrella,resonated;
    public static void Create(string key,Vector3 start,Zombie zombie,bool butter,int targetRow)
    {
        string image=key=="CabbagePult"?"Cabbagepult_cabbage":key=="KernelPult"?(butter?"Cornpult_butter":"Cornpult_kernal"):"Melonpult_melon";
        Sprite sprite=Resources.LoadAll<Sprite>("Sprites/Imported/OriginalPvZ/Pults/Parts").FirstOrDefault(s=>s.name.Equals(image,StringComparison.OrdinalIgnoreCase));
        GameObject go=new GameObject(key+" Projectile",typeof(SpriteRenderer),typeof(OriginalPultProjectile));go.transform.position=start;go.transform.localScale=Vector3.one*2.6f;SpriteRenderer r=go.GetComponent<SpriteRenderer>();r.sprite=sprite;r.sortingLayerName="PlantBullet";r.sortingOrder=5;
        OriginalPultProjectile p=go.GetComponent<OriginalPultProjectile>();p.target=zombie;p.butter=butter;p.melon=key=="MelonPult";p.damage=p.melon?80:(butter?40:key=="CabbagePult"?40:20);p.row=targetRow;p.projectileRenderer=r;
        p.umbrella=FindUmbrella(start,zombie.transform.position,targetRow);
        p.routingToUmbrella=p.umbrella!=null;
        p.SetSegment(start,p.routingToUmbrella?p.umbrella.transform.position+Vector3.up*.42f:zombie.transform.position+Vector3.up*.18f);
        string sound=key=="KernelPult"?(UnityEngine.Random.value<.5f?"kernelpult":"kernelpult2"):(UnityEngine.Random.value<.5f?"throw":"throw2");AudioClip clip=Resources.Load<AudioClip>("Sounds/Plants/Pults/"+sound);if(clip!=null)AudioSource.PlayClipAtPoint(clip,start,.45f);
    }
    private static OriginalPvZPlant FindUmbrella(Vector3 start,Vector3 targetPosition,int targetRow)
    {
        float left=Mathf.Min(start.x,targetPosition.x)+.05f,right=Mathf.Max(start.x,targetPosition.x)-.05f;
        return FindObjectsByType<OriginalPvZPlant>().Where(p=>p.IsUmbrellaSupport&&p.row==targetRow&&p.transform.position.x>left&&p.transform.position.x<right).OrderBy(p=>Mathf.Abs(p.transform.position.x-start.x)).FirstOrDefault();
    }
    private void SetSegment(Vector3 segmentStart,Vector3 segmentEnd)
    {
        from=segmentStart;to=segmentEnd;elapsed=0f;float distance=Mathf.Abs(to.x-from.x);duration=Mathf.Clamp(distance/5.4f,.42f,1.2f);arcHeight=Mathf.Lerp(.62f,1.35f,Mathf.InverseLerp(.42f,1.2f,duration));
    }
    private void Update()
    {
        elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/duration);Vector3 p=Vector3.Lerp(from,to,t);p.y+=Mathf.Sin(t*Mathf.PI)*arcHeight;transform.position=p;transform.Rotate(0,0,-360f*Time.deltaTime);
        if(t<1f)return;
        if(routingToUmbrella&&!resonated){if(umbrella!=null)ActivateResonance();else RerouteWithoutResonance();}else Impact();
    }
    private void ActivateResonance()
    {
        routingToUmbrella=false;resonated=true;umbrella.PlayUmbrellaBlock();damage=Mathf.CeilToInt(damage*ResonanceDamageMultiplier);
        if(projectileRenderer!=null){projectileRenderer.color=new Color(1f,.86f,.28f,1f);ImportedPlantVfx.CreateBurst(transform.position,projectileRenderer.sprite,new Color(1f,.82f,.18f,.9f));}
        transform.localScale*=1.12f;
        target=FindObjectsByType<Zombie>().Where(z=>!z.IsHypnotized&&z.bloodVolume>0&&z.pos_row==row&&z.transform.position.x>umbrella.transform.position.x-.1f).OrderBy(z=>z.transform.position.x).FirstOrDefault();
        umbrella=null;if(target==null){Destroy(gameObject);return;}SetSegment(transform.position,target.transform.position+Vector3.up*.18f);
    }
    private void RerouteWithoutResonance()
    {
        routingToUmbrella=false;if(target==null||target.bloodVolume<=0)target=FindObjectsByType<Zombie>().Where(z=>!z.IsHypnotized&&z.bloodVolume>0&&z.pos_row==row).OrderBy(z=>z.transform.position.x).FirstOrDefault();
        if(target==null){Destroy(gameObject);return;}SetSegment(transform.position,target.transform.position+Vector3.up*.18f);
    }
    private void Impact()
    {
        if(target!=null&&target.bloodVolume>0)
        {
            if(melon)
            {
                CreateMelonImpact(to);
                foreach(Zombie z in FindObjectsByType<Zombie>())if(z.bloodVolume>0&&Vector2.Distance(z.transform.position,to)<.72f)z.beAttacked(z==target?damage:damage/2);
                string impactSound=UnityEngine.Random.value<.5f?"melonimpact":"melonimpact2";
                AudioClip c=Resources.Load<AudioClip>("Sounds/Plants/Pults/"+impactSound);if(c!=null)AudioSource.PlayClipAtPoint(c,to,.65f);
            }
            else
            {
                target.beAttacked(damage);
                if(butter&&target.bloodVolume>0)
                {
                    target.ApplyButterStun(ButterStunDuration);
                    CreateButterSplat(target,ButterStunDuration);
                    AudioClip c=Resources.Load<AudioClip>("Sounds/Plants/Pults/butter");if(c!=null)AudioSource.PlayClipAtPoint(c,to,.65f);
                }
            }
        }
        Destroy(gameObject);
    }

    private static void CreateMelonImpact(Vector3 position)
    {
        if(melonPieces==null)
        {
            Texture2D sheet=Resources.Load<Texture2D>("Sprites/Imported/OriginalPvZ/Pults/Effects/Melonpult_particles");
            if(sheet==null)return;
            melonPieces=new Sprite[9];
            for(int i=0;i<melonPieces.Length;i++)melonPieces[i]=Sprite.Create(sheet,new Rect(i*37,0,37,37),new Vector2(.5f,.5f),250f);
        }
        for(int i=0;i<15;i++)
        {
            GameObject piece=new GameObject("Melon Impact Piece",typeof(SpriteRenderer),typeof(OriginalPultImpactPiece));
            piece.transform.position=position+(Vector3)UnityEngine.Random.insideUnitCircle*.12f;
            piece.transform.localScale=Vector3.one*UnityEngine.Random.Range(1.05f,1.45f);
            SpriteRenderer renderer=piece.GetComponent<SpriteRenderer>();renderer.sprite=melonPieces[UnityEngine.Random.Range(0,melonPieces.Length)];renderer.sortingLayerName="PlantBullet";renderer.sortingOrder=20+i;
            float angle=UnityEngine.Random.Range(25f,155f)*Mathf.Deg2Rad;
            Vector2 velocity=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*UnityEngine.Random.Range(.75f,1.75f);
            piece.GetComponent<OriginalPultImpactPiece>().Configure(renderer,velocity,UnityEngine.Random.Range(-520f,520f),UnityEngine.Random.Range(.42f,.68f));
        }
    }

    private static void CreateButterSplat(Zombie zombie,float lifetime)
    {
        Transform old=zombie.transform.Find("Butter Splat");if(old!=null)Destroy(old.gameObject);
        Sprite sprite=Resources.LoadAll<Sprite>("Sprites/Imported/OriginalPvZ/Pults/Parts").FirstOrDefault(s=>s.name.Equals("Cornpult_butter_splat",StringComparison.OrdinalIgnoreCase));
        if(sprite==null)return;
        SpriteRenderer[] zombieRenderers=zombie.GetComponentsInChildren<SpriteRenderer>();
        Bounds bounds=new Bounds(zombie.transform.position,Vector3.one*.5f);bool found=false;int order=0;string layer="PlantBullet";
        foreach(SpriteRenderer zr in zombieRenderers)if(zr.enabled&&zr.sprite!=null){if(!found){bounds=zr.bounds;found=true;}else bounds.Encapsulate(zr.bounds);if(zr.sortingOrder>=order){order=zr.sortingOrder;layer=zr.sortingLayerName;}}
        GameObject splat=new GameObject("Butter Splat",typeof(SpriteRenderer),typeof(OriginalTimedImpactVisual));
        splat.transform.position=new Vector3(bounds.center.x,bounds.center.y+bounds.extents.y*.55f,zombie.transform.position.z-.15f);splat.transform.SetParent(zombie.transform,true);splat.transform.localScale=Vector3.one*1.25f;
        SpriteRenderer renderer=splat.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingLayerName=layer;renderer.sortingOrder=order+20;
        splat.GetComponent<OriginalTimedImpactVisual>().Configure(lifetime);
    }
}

public sealed class OriginalPultImpactPiece : MonoBehaviour
{
    private SpriteRenderer renderer;private Vector2 velocity;private float spin,lifetime,age;
    public void Configure(SpriteRenderer value,Vector2 initialVelocity,float angularSpeed,float seconds){renderer=value;velocity=initialVelocity;spin=angularSpeed;lifetime=seconds;}
    private void Update(){age+=Time.deltaTime;velocity+=Vector2.down*3.2f*Time.deltaTime;transform.position+=(Vector3)(velocity*Time.deltaTime);transform.Rotate(0,0,spin*Time.deltaTime);float alpha=1f-Mathf.Clamp01(age/lifetime);renderer.color=new Color(1,1,1,alpha);if(age>=lifetime)Destroy(gameObject);}
}

public sealed class OriginalTimedImpactVisual : MonoBehaviour
{
    private float lifetime;
    public void Configure(float seconds){lifetime=seconds;}
    private void Update(){lifetime-=Time.deltaTime;if(lifetime<=0f)Destroy(gameObject);}
}
