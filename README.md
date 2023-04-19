## Qplaze.SimpleHeirs package ##
Цей пакет дозволяє просто та зручно вибирати в інспекторі та серіалізувати будь-якого нащадка класу чи інтерфейсу, який ви вказуєте, та після взаємодіяти з ним у коді.		

Розробник: <danil.sigaev@qplaze.com>		

***Попередження:*** *в прикладах я не дотримувався якогось чіткого стилю, щоб продемонструвати більше інформації та використати менше коду, будь уважним :)*		

### Навіщо пакет потрібен? ###
Коротка відповідь: за для поліморфізму! 
Взагалі проблема поліморфізму вирішується багатьма способами, найпоширеніший це використання принципу [Inversion of control](https://en.wikipedia.org/wiki/Inversion_of_control). Але на превеликий жаль, нам часто трапляються проекти в яких через архітектуру проблематично використовувати методи "[впровадження залежностей](https://en.wikipedia.org/wiki/Dependency_injection)". Дефолтне рішення для поліморфізму у юніті це безліч **MonoBehaviour-ів** які зв'язані один з одним через інспектор, але все одно ще не можна використовувати чисті інтерфейси, та не залежати від чіткого базового класу (MonoBehaviour або ScriptableObject).
Через таку проблему і був створений цей пакет. Він додає можливість працювати з абстракціями у коді, а залежності впроваджувати через інспектор (взагалі це вважається не дуже гарною практикою, але краще ніж прямі залежності).

### Трохи теорії ###
Для того щоб Unity міг працювати з полями класу в інспекторі, цей клас має бути серіалізованим. Також данні серіалізованих класів зберігаються поміж сесіями, гарячими перезавантаженнями, тощо. Більше про це: [тут](https://docs.unity3d.com/Manual/script-Serialization.html#SerializationRules). Кожен клас спадкоємець від **UnityEngine.Object**, за замовчуванням серіалізований, але якщо тобі потрібно використовувати не **UnityEngine** об'єкти, то зробити звичайний клас серіалізованим можна за допомогою атрибута **[System.Serializable]**. Також, за замовчуванням, система серіалізації в юніті вважає серіалізованими лише публічні поля, але за допомогою атрибуту **[UnityEngine.SerializeField]** серіалізованими можуть стати поля з будь яким модифікатором доступу. 
Наприклад: 
 - Тут клас **Shape** **не** серіалізований, тому жодної інформації про нього не буде відображено у інспекторі:
```cs
public class Shape 
{
    public string Name;

    private float _area;
}
```
 - Тут клас **Shape** серіалізований, тому у інспекторі буде відображено лише поле **Name**:
```cs
[System.Serializable]
public class Shape 
{
    public string Name;

    private float _area;
}
```
 - Тут клас **Shape** серіалізований, а також явно серіалізованим помічено поле **_area**, тому у інспекторі буде відображено і **Name**, і **_area**:
```cs
[System.Serializable]
public class Shape 
{
    public string Name;

    [UnityEngine.SerializeField] private float _area;
}
```

### Як використовувати? ###
Все дуже просто. Для цього в нас є клас **HeirsProvider**. Він "**generic**" з параметром типу **T**, саме у цей параметр ми і маємо передати той тип даних або інтерфейс, з нащадками або реалізаціями котрих ми хочемо "робити поліморфізм". 
Важливо пам'ятати, що щоб нащадок типу **Т** відображався і зберігався, він має бути серіалізованим!
Наприклад:
- Уявімо в нас є клас який буде кожні декілька секунд виводити якусь фігуру у консоль:
```cs
public abstract class Shape
{
    public string Name;
	public float Scale;

    public abstract string GetShape();
}

public class ShapeDrawer : MonoBehaviour
{
    [SerializeField] private Shape _shape;

    protected IEnumerator Start()
    {
        while (true)
        {
            Debug.Log(_shape.GetShape());
            yield return new WaitForSeconds(2f);
        }
    }
}
```
- По перше, у інспекторі не буде нічого відображено, по друге, взагалі станеться помилка бо не може існувати екземпляру абстрактного класу.
- Проблема полягає у тому, що ми не хочемо робити **Shape** нащадком **MonoBehaviour**, щоб фігури залишалися **не ігровими** об'єктами, а простою поведінкою з необхідними даними.
- Вирішити цю проблему можна таким шляхом:
```cs
public class ShapeDrawer : MonoBehaviour
{
    [SerializeField] private HeirsProvider<Shape2D> _shape;

    private Shape2D _shapeValue;

    protected IEnumerator Start()
    {
        _shapeValue = _shape.GetValue();
        while (true)
        {
            if (_shapeValue != null)
            {
                Debug.Log(_shapeValue.GetShape());
            }
            yield return new WaitForSeconds(2f);
        }
    }
}
```
- *Зверни увагу на те як змінились назви змінних!*
- Щоб отримати екземпляр нащадка **Shape** (до речі, там міг би бути екземпляр і самого **Shape**, як би він не був абстрактним) потрібно звернутися до метода **GetValue()**.
- Як і в прикладі, гарною практикою буде закешувати результат метода **GetValue()**, бо "під капотом" відбуваються перевірки на **null**, та перетворення типів, що наприклад у **Update-і** буде впливати на продуктивність.
- До речі, ось що тепер видно у інспекторі:

![EmptyShapesInInspector](https://lh3.googleusercontent.com/u/3/drive-viewer/AAOQEOSduRT5x4Ln14aGLzwAsQv3GedlDK7UJp5VmOzs3_TXaeKPjv0hoWNAWhbvFFOLIV7DG79JZyPKVvpXt0p4apaxGU2YcQ=w1920-h901)

- Створимо декілька нових нащадків для класу **Shape**: 
```cs
[Serializable]
public class Polygon : Shape
{
    [SerializeField] internal int CornersNuber;

    public override string GetShape()
        => $"Polygon: \"{Name}\"; Scale: {Scale}; Corners number: {CornersNuber};";
}

[Serializable]
public class Circle : Shape
{
    [SerializeField] protected float Radius;

    public override string GetShape()
        => $"Circle: \"{Name}\"; Scale: {Scale}; Radius: {Radius};";
}

[Serializable]
public class CustomArea : Shape
{
    [SerializeField] private List<Vector2> EdgePoints;

    public override string GetShape()
        => $"CustomArea: \"{Name}\"; Scale: {Scale}; Edge points: {EdgePoints.Count};";
}

[Serializable]
public class MultiShape : Shape
{
    public List<HeirsProvider<Shape>> Shapes;

    public override string GetShape()
    {
        string shapes = "\n -> " + string.Join("\n -> ", 
            Shapes.Where(x => x.GetValue() != null).Select(x => x.GetValue().GetShape()));

        return $"MultiShape: \"{Name}\"; Scale: {Scale};\n" +
            $"Contained shapes: \n{shapes};";
    }
}
```
- Після цього, ці класи автоматично додадуться до списку у інспекторі:

![EmptyShapesInInspector](https://lh3.googleusercontent.com/u/3/drive-viewer/AAOQEOQEK2ER_6ndwhFZN1xzQsAycvH8BivL9IiYN01808vzHo24qbo0wm6dUFIk_pR_gYZDCgrLZI9JjfZpvddj3l6xTbyQOQ=w1920-h901)

- Створимо **ShapeDrawer**, для кожного варіанту Shape:

![AllShapesDrawers](https://lh3.googleusercontent.com/u/3/drive-viewer/AFGJ81qjPPwKZw9DrjEFyeCXLZPhvH6Zxq4UQvO4ra8FUdS891iA6Tj8Tt8RQpUngLhCY8WRhm30hiZBeh2esvWpWyd0tADnKw=w1920-h901)

- Зверни увагу на **ShapeDrawer** для **MultiShape**, він містить список об'єктів типу **Shape**. А це означає, що:
    1.  **HeirsProvider** може містити та відображати об'єкти з іншими **HeirsProvider**, включаючи **HeirsProvider** з тим же типом, тобто підтримує і рекурсію. Це продемонстровано у червоній рамці, потенційно там ми могли вибрати **MultiShape** ще раз, а у ньому і ще раз, і ще...
    2.  **HeirsProvider** може працювати зі списками, як бути в середині списку, так і містити його. *(На разі при роботі зі списками в інспекторі виникають візуальні баги, наприклад коли змінюється порядок елементів. Це виправляється простим оновленням інспектору, або натискання Foldout стрілки зліва)*

- Ось який результат ми побачимо у консолі:
![ShapeDrawersConsoleResult](https://lh3.googleusercontent.com/u/3/drive-viewer/AAOQEOS8rqIt3cyFns7LPElJsv6FNFwW3PNnarK6GhCU80oCA8ehzfUlW3_rr1PmLm6XvAjx7ArFoZ9VDILiH22FqdB8w7h16w=w1920-h901)

- Створимо новий клас, але який тепер може користуватись інтерфейсом **IGun**:
```cs
public interface IGun
{
    public void Shoot();
}

public class GunUser : MonoBehaviour
{
    [SerializeField] private HeirsProvider<IGun> _gun;

    private IGun _gunValue;

    protected IEnumerator Start()
    {
        _gunValue = _gun.GetValue();
        while (true)
        {
            if (_gunValue != null)
            {
                _gunValue.Shoot();
            }
            yield return new WaitForSeconds(2f);
        }
    }
}

```
- І декілька реалізацій для нового інтерфейсу:
```cs
[Serializable]
public class SimpleGun : IGun
{
    [SerializeField] private float _damage;

    public void Shoot()
    {
        Debug.Log("Shoot by SimpleGun!");
    }
}

public class MonoGun : MonoBehaviour, IGun
{
    public int ShootRange;

    public void Shoot()
    {
        Debug.Log("Shoot by MonoGun!");
    }
}

[CreateAssetMenu(fileName = "ScriptableGun",
    menuName = "SimpleHeirsTest/ScriptableGun")]
public class ScriptableGun : ScriptableObject, IGun
{
    public int SplashRadius;

    public void Shoot()
    {
        Debug.Log("Shoot by ScriptableGun!");
    }
}
```
- Ось так буде виглядати **GunUser** для кожної реалізації **IGun**:

![GunUsersInspector](https://lh3.googleusercontent.com/u/3/drive-viewer/AFGJ81rf8uNX6D64oeoT7sGoKdL_iKioxyDTFp4n9Qd_X3uo6WjUj1CE9T2R7YuLeGrCv8uhfNfmuBnJ4rR1Y6c18Q2Nv3hy=w1920-h901)

- Таке в нас буде у консолі:

![GunUsersConsoleLog](https://lh3.googleusercontent.com/u/3/drive-viewer/AAOQEORSA9S-GzW1Oi7Aw_LmXtOl19u0tlR9nMXBVtC0sjqQXnKzGbkeGmZv2XKmfz0EjEhtojyZKtL5ozJm7GnpzPUTzphDzw=w1920-h901)


### Можливі проблеми: ###
1. Якщо клас був перейменований, переміщений або видалений, то всі серіалізовані данні будуть видалені, тобто усі поля з **HeirsProvider** який містить цей клас будуть встановлені як **Empty**. Важливі данні краще тримати у звичайному **ScriptableObject**, та вже його передавати до поля з **HeirsProvider**.
2. **HeirsProvider** наразі має багато візуальних багів при роботі у списках, вони не критичні, їх можна позбутися перезавантаженням інспектору, але потрібно бути готовим до цього. (Очевидне рішення, це створити окремий **PropertyDrawer** для відображення списку з **HeirsProvider**, але поки він не готовий.)
3. Проблеми з продуктивністю коли в інспекторі відображається велика структура, або довгий список структур, тому бажано "закривати" відображення структури Foldout стрілкою після завершення зміни даних. 