using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;

public class MapManagerScript : MonoBehaviour
{
    [SerializeField] GameObject currentPlane;
    [SerializeField] GameObject planePrefab;
    [SerializeField] GameObject obstaclePrefab;
    [SerializeField] float obstacleChance;
    [SerializeField] List<GameObject> currentAdjacentPlanes;
    [SerializeField] List<GameObject> visibleTiles;
    [SerializeField] List<Vector3> visiblePoints;
    [SerializeField] float cellSpacing;
    [SerializeField] Dictionary<Vector3, GameObject> posOfPlanes;
    [SerializeField] List<Vector3> positions;
    [SerializeField] GameObject player;
    [SerializeField] PlayerScript playerScript;
    [SerializeField] Material[] materials;
    [SerializeField] int discoverArea;
    [SerializeField] int poolQuantity;
    [SerializeField] int remainingInactivePlanes;
    [SerializeField] int remainingInactiveObstacles;
    [SerializeField] List<GameObject> planesPool = new List<GameObject>();
    [SerializeField] List<GameObject> obstaclesPool = new List<GameObject>();
    [SerializeField] int RaysToShoot;
    [SerializeField] CinemachineVirtualCamera vCamera;
    // Start is called before the first frame update
    void Start()
    {
        posOfPlanes = new Dictionary<Vector3, GameObject>();
        posOfPlanes[Vector3.zero] = currentPlane;
        positions = new List<Vector3>();
        visibleTiles = new List<GameObject>();
        playerScript = player.GetComponent<PlayerScript>();
        poolQuantity = discoverArea * 30;
        Pool(planePrefab, poolQuantity);
        remainingInactivePlanes = poolQuantity;
        Pool(obstaclePrefab, poolQuantity);
        remainingInactiveObstacles = poolQuantity;
        UpdatePlane(player.transform.position);
    }
    public void UpdatePlane(Vector3 newPos)
    {
        Vector3 followOffset = new Vector3(0,6*discoverArea,-2);
        vCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = followOffset;
        GameObject newPlane = posOfPlanes[RoundVector3(newPos)];
        currentPlane = newPlane;
        poolQuantity = discoverArea * 30;
        StartCoroutine(SummonPlanes(newPos));
    }
    public bool CheckCellifEmpty(Vector3 pos)
    {
        return (!posOfPlanes[RoundVector3(pos)].name.StartsWith("Obstacle"));
    }
    void PaintPlanes(Vector3 playerPosition)
    {
        float sphereradius = discoverArea * 10;
        string tileLayer = "Tiles";
        int tileLayerMask = 1 << LayerMask.NameToLayer(tileLayer);
        Collider[] hits = Physics.OverlapSphere(playerPosition, sphereradius * 3, tileLayerMask);
        foreach (Collider hit in hits)
        {
            GameObject goHit = hit.transform.gameObject;
            float distanceToPlayer = Vector3.Distance(playerPosition, hit.transform.position);
            MeshRenderer planeRenderer = goHit.GetComponent<MeshRenderer>();
            if (!visibleTiles.Contains(goHit))
            {
                planeRenderer.material = materials[4];
            }
            else
            {
                if (currentAdjacentPlanes.Contains(goHit))
                {
                    planeRenderer.material = (distanceToPlayer < sphereradius / 4) ? materials[0] : materials[1];
                }
                else
                {
                    planeRenderer.material = (distanceToPlayer < sphereradius) ? materials[2] : materials[3];
                }
            }
        }
        StopCoroutine(SummonPlanes(playerPosition));
    }
    void setVisible(Vector3 playerPosition)
    {
        RaysToShoot = 12 * discoverArea;
        visibleTiles = new List<GameObject>();
        //List<Vector3> nowVisiblePoints = new List<Vector3>();
        float angleIncrement = 360f / RaysToShoot;
        float currentAngle = 0f;
        visibleTiles.Add(posOfPlanes[RoundVector3(playerPosition)]);
        for (int i = 1; i <= RaysToShoot; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * currentAngle);
            float z = Mathf.Cos(Mathf.Deg2Rad * currentAngle);
            string tileLayer = "Tiles";
            int tileLayerMask = 1 << LayerMask.NameToLayer(tileLayer);
            Vector3 dir = new Vector3(x, 0f, z);
            RaycastHit[] hits = Physics.RaycastAll(playerPosition, dir, discoverArea * 12, tileLayerMask);
            hits = hits.OrderBy(hit => hit.distance).ToArray();
            foreach (RaycastHit hit in hits)
            {
                GameObject found = posOfPlanes[RoundVector3(hit.transform.position)];
                if (found.name.StartsWith("Obsta"))
                {
                    visibleTiles.Add(posOfPlanes[RoundVector3(found.transform.position)]);
                    break;
                }
                if (!visibleTiles.Contains(found))
                {
                    visibleTiles.Add(found);
                }
            }
            currentAngle += angleIncrement;
        }
        //Debug.Log("Visible: "+string.Join(", ",visiblePoints.Select(x => x.ToString())));
        PaintPlanes(playerPosition);
    }
    IEnumerator SummonPlanes(Vector3 centralPoint)
    {
        /*foreach (GameObject go in currentAdjacentPlanes)
        {
            //Destroy(go);
        }*/
        centralPoint = RoundVector3(centralPoint);
        int seeder = 0;
        currentAdjacentPlanes.Clear();
        for (int row = -discoverArea; row <= discoverArea; row++)
        {
            int spaces = Mathf.Abs(discoverArea - Mathf.Abs(row));
            for (int col = -discoverArea; col <= discoverArea; col++)
            {
                seeder++;
                //Random.InitState(playerScript.stepsTaken+seeder);
                if (col < -spaces || col > spaces)
                {
                    // Skip the corner cells
                    continue;
                }
                // Set 'X' in the cells within the diamond shape
                if (row == 0 && col == 0)
                {
                    currentAdjacentPlanes.Add(posOfPlanes[centralPoint]);
                    continue; // Skip the target position
                }
                InstantiateNew(centralPoint, row, col, obstaclePrefab, planePrefab);
            }
        }
        yield return new WaitForSeconds(0.01f);
        setVisible(centralPoint);
    }
    int CustomSign(int x)
    {
        return (int)((x != 0) ? Mathf.Sign(x) : 0);
    }
    void InstantiateNew(Vector3 pos, float x, float z, GameObject obstacle, GameObject plane)
    {
        Vector3 newPos = RoundVector3(pos + new Vector3(x * cellSpacing, 0f, z * cellSpacing));
        if (!posOfPlanes.ContainsKey(newPos))
        {
            int randNumber = Random.Range(0, 100);
            //Debug.Log("rn= "+randNumber +" - Will spawn? "+(randNumber < obstacleChance));
            GameObject selectedGameObject = (randNumber < obstacleChance / (discoverArea * 2)) ? obstacle : plane;
            GameObject newGo = GetPooledObject(selectedGameObject);
            newGo.SetActive(true);
            newGo.transform.position = newPos;
            Vector3 intPos = RoundVector3(newGo.transform.position);
            positions.Add(intPos);
            posOfPlanes[intPos] = newGo;
        }
        currentAdjacentPlanes.Add(posOfPlanes[newPos]);
    }
    public Vector3 RoundVector3(Vector3 vector3)
    {
        return new Vector3(
            (int)vector3.x,
            (int)vector3.y,
            (int)vector3.z);
    }
    // Update is called once per frame
    void Update()
    {

    }
    void Pool(GameObject tile, int max)
    {
        for (int i = 0; i <= max; i++)
        {
            GameObject newGo = Instantiate(tile);
            newGo.SetActive(false);
            //Debug.Log("tile.name "+tile.name);
            switch (tile.name)
            {
                case "Plane":
                    {
                        planesPool.Add(newGo);
                        break;
                    }
                case "Obstacle":
                    {
                        obstaclesPool.Add(newGo);
                        break;
                    }
            }
        }
    }
    GameObject GetPooledObject(GameObject toGet)
    {
        switch (toGet.name.Substring(0, 5))
        {
            case "Plane":
                {
                    foreach (GameObject plane in planesPool)
                    {
                        if (!plane.activeInHierarchy)
                        {
                            remainingInactivePlanes = planesPool.Where(go => !go.activeInHierarchy).ToList().Count;
                            if (remainingInactivePlanes < discoverArea * 3)
                            {
                                //Debug.Log("Refill planes");
                                Pool(planePrefab, poolQuantity);
                            }
                            return plane;
                        }
                    }
                    break;
                }
            case "Obsta":
                {
                    foreach (GameObject obstacle in obstaclesPool)
                    {
                        if (!obstacle.activeInHierarchy)
                        {
                            remainingInactiveObstacles = obstaclesPool.Where(go => !go.activeInHierarchy).ToList().Count;
                            if (remainingInactiveObstacles < discoverArea * 3)
                            {
                                //Debug.Log("Refill obstacles");
                                Pool(obstaclePrefab, poolQuantity);
                            }
                            return obstacle;
                        }
                    }
                    break;
                }
        }
        return null;
    }
}
