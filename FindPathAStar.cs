using UnityEngine;
using System.Linq;
using System.Collections.Generic;
// PathMarker sınıfı, A* algoritmasında bir düğümü temsil eder.
// Labirent üzerinde işaretler ekler
public class PathMarker
{
    public MapLocation location; // İşaretin konumu (labirent üzerindeki koordinatlar).
    public float G;// Başlangıç noktasından bu noktaya olan maliyet
    public float H;// Bu noktadan hedefe olan tahmini maliyet
    public float F;// G ve H değerlerinin toplamı (F = G + H).
    public GameObject marker;// Bu noktanın oyun nesnesi
    public PathMarker parent;// Bu noktanın ebeveyn noktası
    // PathMarker yapıcı metodu
    public PathMarker(MapLocation l, float g, float h, float f, GameObject marker, PathMarker p)
    {
        location = l;
        G = g;
        H = h;
        F = f;
        this.marker = marker;
        parent = p;
    }
    // Eşitlik kontrolü
    public override bool Equals(object obj) {
    if ((obj == null) || !this.GetType().Equals(obj.GetType())) { 
        return false; 
    } 
    else {
        return location.Equals(((PathMarker) obj).location);}
    }
    // Hash kodu oluşturma
    public override int GetHashCode() {
        return 0;
    }
}
// FindPathAStar sınıfı, Unity'de A* algoritmasını kullanarak bir labirentte yol bulma işlemini gerçekleştirir.
public class FindPathAStar : MonoBehaviour
{
    public Maze maze;// Labirent referansı
    public Material closedMaterial;// Kapalı noktaların materyali
    public Material openMaterial;// Açık noktaların materyali
    List<PathMarker> open = new List<PathMarker>();// Açık noktaların listesi
    List<PathMarker> closed = new List<PathMarker>();// Kapalı noktaların listesi
    public GameObject start;// Başlangıç noktası oyun nesnesi
    public GameObject end;// Bitiş noktası oyun nesnesi
    public GameObject pathP;// Yol noktası oyun nesnesi
    PathMarker goalNode;// Hedef nokta
    PathMarker startNode;// Başlangıç nokta
    PathMarker lastPos;// Son işlenen nokta
    bool done = false;// Aramanın tamamlanıp tamamlanmadığını belirten bayrak
    // Tüm işaretçileri kaldırır
    void RemoveAllMarkers()
    {
        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");
        foreach(GameObject m in markers)
            Destroy(m);
    }
    // Aramayı başlatır
    void BeginSearch() {
        done = false;
        RemoveAllMarkers();
        List<MapLocation> locations = new List<MapLocation>();
        for (int z=1; z<maze.depth-1; z++)
            for (int x=1; x<maze.depth-1; x++)
            {
                if (maze.map[x, z] != 1) //0: empty space, 1: wall
                    locations.Add(new MapLocation(x,z));
            }
        locations.Shuffle();

        //0 for G, H, and F values. Null for the parent.
        Vector3 startLocation = new Vector3(locations[0].x * maze.scale, 0, locations[0].z * maze.scale);
        startNode = new PathMarker(new MapLocation(locations[0].x, locations[0].z), 0, 0, 0,
        Instantiate(start, startLocation, Quaternion.identity), null);

        Vector3 goalLocation = new Vector3(locations[1].x * maze.scale, 0, locations[1].z * maze.scale);
        goalNode = new PathMarker(new MapLocation(locations[1].x, locations[1].z), 0, 0, 0,
        Instantiate(end, goalLocation, Quaternion.identity), null);

        open.Clear();
        closed.Clear();
        open.Add(startNode);
        lastPos = startNode;
    }
    // Arama işlemini gerçekleştirir
    void Search(PathMarker thisNode)
    {
        if (thisNode == null) return;
        if (thisNode.Equals(goalNode)) 
        { 
            done = true; 
            return; 
        }
        // Komşular yatay ve dikeydir, ancak çaprazları da ekleyebiliriz. Maze scriptine bakabilirsiniz.
        foreach(MapLocation dir in maze.directions)
        {
            MapLocation neighbor = dir + thisNode.location;
            if (maze.map[neighbor.x, neighbor.z] == 1) continue; // duvarları atla
            if (neighbor.x < 1 || neighbor.x >= maze.width || neighbor.z < 1 || neighbor.z >= maze.depth) continue;
            if (IsClosed(neighbor)) continue;

            float G = Vector2.Distance(thisNode.location.ToVector(), neighbor.ToVector()) + thisNode.G;
            float H = Vector2.Distance(neighbor.ToVector(), goalNode.location.ToVector());
            float F = G + H;

            GameObject pathBlock = Instantiate(pathP, new Vector3(neighbor.x * maze.scale, 0, neighbor.z * maze.scale), Quaternion.identity);
            TextMesh[] values = pathBlock.GetComponentsInChildren<TextMesh>();
            // Prefab'de, G ilk eklenir, ardından H ve ardından F. Prefab'i kontrol edebilirsiniz.
            values[0].text = "G: " + G.ToString("0.00");
            values[1].text = "H: " + G.ToString("0.00");
            values[2].text = "F: " + G.ToString("0.00");
            if (!UpdateMarker(neighbor, G, H, F, thisNode))
                open.Add(new PathMarker(neighbor, G, H, F, pathBlock, thisNode));
        }
         // En küçük F değerine sahip olanı seç
        // Birden fazla F değeri varsa, en küçük H değerine sahip olanı seç
        open = open.OrderBy(p => p.F).ThenBy(n => n.H).ToList<PathMarker>();
        PathMarker pm = (PathMarker) open.ElementAt(0);
        closed.Add(pm);
        open.RemoveAt(0);
        pm.marker.GetComponent<Renderer>().material = closedMaterial;
        lastPos = pm;
    }
    // Bir noktanın kapalı listede olup olmadığını kontrol eder
    bool IsClosed(MapLocation marker)
    {
        foreach (PathMarker p in closed)
        {
            if (p.location.Equals(marker)) return true;
        }
        return false;
    }
    // Bir işaretçiyi günceller
    bool UpdateMarker(MapLocation pos, float g, float h, float f, PathMarker prt) {
        foreach (PathMarker p in open) 
        {
            if (p.location.Equals(pos)) 
            {
                p.G = g;
                p.H = h;
                p.F = f;
                p.parent = prt;
                return true;
            }
        }
        return false;
    }
    // Bulunan yolu alır
    void GetPath() {
        RemoveAllMarkers();// Tüm işaretçileri kaldırır
        PathMarker begin = lastPos;// Son işlenen noktayı başlangıç olarak alır
        while (!startNode.Equals(begin) && begin != null) // Başlangıç noktasına ulaşana kadar ve geçerli nokta null olmadığı sürece döngüye devam eder
        {
            // Geçerli noktanın konumunda bir yol noktası oluşturur
            Instantiate(pathP, new Vector3(begin.location.x * maze.scale, 0, begin.location.z * maze.scale), Quaternion.identity);
            begin = begin.parent;// Bir önceki noktaya geçer
        }
        // Başlangıç noktasında bir yol noktası oluşturur
        Instantiate(pathP, new Vector3(startNode.location.x * maze.scale, 0, startNode.location.z * maze.scale), Quaternion.identity);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    // Update her karede bir kez çağrılır
    void Update()
    {
         // P tuşuna basıldığında aramayı başlatır
        if (Input.GetKeyDown(KeyCode.P)) BeginSearch();
        // C tuşuna basıldığında ve arama tamamlanmadıysa, son işlenen noktadan itibaren aramaya devam eder
        if (Input.GetKeyDown(KeyCode.C) && !done) Search(lastPos);
        // M tuşuna basıldığında bulunan yolu işaretler
        if (Input.GetKeyDown(KeyCode.M)) GetPath();
    }
}