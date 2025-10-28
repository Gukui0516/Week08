using UnityEngine;

public class CloudMove : MonoBehaviour
{
    [SerializeField] float speedmin = 1;
    [SerializeField] float speedmax = 1;
    float speed;
    Color color;
    bool regen;
    Renderer rend;
    [SerializeField] public int genPosXmin = -200;
    [SerializeField] public int genPosXmax = 200;
    [SerializeField] public int genPosZmin = -200;
    [SerializeField] public int genPosZmax = 200;
    [SerializeField] public int genPosYmin = 20;
    [SerializeField] public int genPosYmax = 25;

    [SerializeField] float fadeTime = 0.1f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        speed = Random.Range(speedmin, speedmax);
        rend = GetComponent<Renderer>();
        rend.material = new Material(rend.material); 
        color = rend.material.GetColor("_Color");
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += Vector3.forward*speed*Time.deltaTime;
        if (transform.position.z > genPosZmax)
        {
            color.a -= Time.deltaTime*speed* fadeTime; //알파 따로 계산 후
            rend.material.SetColor("_Color", color);//적용
        }
        if (regen)//재성성중
        {
            color.a += Time.deltaTime * speed * fadeTime;
            rend.material.SetColor("_Color", color);
            if (color.a >= 1)
            {
                regen = false;
            }

        }
        else if (color.a <= 0)
        {
            ReGenerate();
            regen=true;
        }
    }
    void ReGenerate()
    {
        float randomX = Random.Range(genPosXmin, genPosXmax);
        float randomY = Random.Range(genPosYmin, genPosYmax);
        speed = Random.Range(speedmin, speedmax);//속도 새로
        transform.position = new Vector3(randomX, randomY, genPosZmin);
    }
}
