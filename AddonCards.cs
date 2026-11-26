using System;
using System.Collections.Generic;
using UnityEngine;

// Карты аддона: The Binding of Isaac и S.T.A.L.K.E.R. Добавляются в каталог основного мода (CardCatalog).
namespace RoundsSettingsMod
{
    public static class AddonCards
    {
        const string Common = "Common", Unc = "Uncommon", Rare = "Rare";
        static string Pct(float f) { return (f >= 0 ? "+" : "") + Mathf.RoundToInt(f * 100f) + "%"; }
        static string F1(float x) { return x.ToString("0.#").Replace(',', '.'); }

        static Eff Mul(char c, string f, float v, string label, bool higherIsGood)
        {
            return new Eff { Comp = c, Field = f, Value = v, Label = label, Amount = Pct(v - 1f), Positive = higherIsGood ? v >= 1f : v <= 1f };
        }
        // attackSpeed — задержка между выстрелами: меньше = быстрее
        static Eff Rate(float v) { return new Eff { Comp = 'G', Field = "attackSpeed", Value = v, Label = "Скорострельность", Amount = Pct(1f / v - 1f), Positive = v <= 1f }; }
        static Eff Add(char c, string f, float v, string label, string amount, bool positive)
        {
            return new Eff { Comp = c, Field = f, Value = v, Label = label, Amount = amount, Positive = positive };
        }

        static Eff Dmg(float v) { return Mul('G', "damage", v, "Урон", true); }
        static Eff Hp(float v) { return Mul('P', "health", v, "Здоровье", true); }
        static Eff Speed(float v) { return Mul('P', "movementSpeed", v, "Скорость", true); }
        static Eff Jump(float v) { return Mul('P', "jump", v, "Высота прыжка", true); }
        static Eff Grav(float v) { return Mul('P', "gravity", v, "Гравитация", false); }
        static Eff Size(float v) { return Mul('P', "sizeMultiplier", v, "Размер", false); }
        static Eff Spread(float v) { return Mul('G', "multiplySpread", v, "Разброс", false); }
        static Eff BSpeed(float v) { return Mul('G', "projectileSpeed", v, "Скорость пули", true); }
        static Eff Knock(float v) { return Mul('G', "knockback", v, "Отбрасывание", true); }
        static Eff Reload(float v) { return Mul('G', "reloadTime", v, "Перезарядка", false); }
        static Eff ReloadAdd(float s) { return Add('G', "reloadTimeAdd", s, "Перезарядка", (s >= 0 ? "+" : "") + F1(s) + "с", s <= 0f); }
        static Eff Ammo(int n) { return Add('G', "ammo", n, "Патроны", (n >= 0 ? "+" : "") + n, n >= 0); }
        static Eff Bullets(int n) { return Add('G', "numberOfProjectiles", n, "Пули", (n >= 0 ? "+" : "") + n, n >= 0); }
        static Eff Bounce(int n) { return Add('G', "reflects", n, "Отскоки", (n >= 0 ? "+" : "") + n, n >= 0); }
        static Eff Jumps(int n) { return Add('P', "numberOfJumps", n, "Прыжки", (n >= 0 ? "+" : "") + n, n >= 0); }
        static Eff Regen(float x) { return Add('P', "regen", x, "Регенерация", "+" + F1(x) + " HP/с", true); }
        static Eff Steal(float x) { return Add('P', "lifeSteal", x, "Вампиризм", Pct(x), true); }
        static Eff Respawn(int n) { return Add('P', "respawns", n, "Воскрешения", "+" + n, true); }
        static Eff Slow(float x) { return Add('G', "slow", x, "Замедление цели", Pct(x), true); }
        // строка в карте без изменения статов (механику выполняет Isaac.cs)
        // «метка»: только строка в карте (механику выполняет Isaac.cs); безвредно записывает 0 в ammoReg
        static Eff Tag(string label, string amount) { return new Eff { Comp = 'G', Field = "ammoReg", Value = 0f, Label = label, Amount = amount, Positive = true }; }
        static Eff AmmoReg(float x) { return Add('G', "ammoReg", x, "Патроны за секунду", "+" + F1(x), true); }
        static Eff SmartB(int n) { return Add('G', "smartBounce", n, "Наводящиеся отскоки", "+" + n, true); }
        // входящий урон растягивается во времени (мягче удары)
        static Eff TakeOver(float s) { return Add('P', "secondsToTakeDamageOver", s, "Урон растягивается", "+" + F1(s) + "с", true); }

        static void C(int num, string id, string name, string desc, string rarity, params Eff[] effs)
        {
            var d = new CardDef { Num = num, Id = "RS_" + id, Name = name, Desc = desc, Effs = new List<Eff>(effs) };
            d.Rarity = rarity;
            if (CardCatalog.ById.ContainsKey(d.Id)) return;
            CardCatalog.All.Add(d); CardCatalog.ById[d.Id] = d;
        }

        public static void Add()
        {
            // --- The Binding of Isaac: настоящие механики (см. Isaac.cs). «Активка» — клавиша Q (геймпад: Y) ---
            // атаки
            C(0, "i_sadonion", "Грустный лук", "Слёзы чаще — стреляешь заметно быстрее", Common, Rate(0.7f));
            C(0, "i_brimstone", "Бримстоун", "Выстрел превращается в кровавый лазер: пробивает всех на линии, урон x1.6, стреляешь реже", Rare, Tag("Лазер", "x1.6 урон"), Rate(1.6f));
            C(0, "i_technology", "Технология", "Выстрел — тонкий лазер через всю арену, урон x0.7, но часто", Unc, Tag("Тонкий лазер", "x0.7 урон"), Rate(0.85f));
            C(0, "i_momsknife", "Нож мамы", "Вместо пули — короткий клинок перед тобой (дальность 4.5), быстро и больно", Unc, Tag("Клинок", "дальность 4.5"), Dmg(1.3f), Rate(0.6f));
            C(0, "i_drfetus", "Доктор Фетус", "Пули взрываются при попадании: урон по области и отбрасывание", Unc, Tag("Взрыв", "радиус 3.5"), Rate(1.4f), BSpeed(0.8f));
            C(0, "i_epicfetus", "Эпический плод", "Ракета: большая пуля, огромный взрыв радиусом 6, редкие выстрелы", Rare, Tag("Большой взрыв", "радиус 6"), Rate(1.8f), BSpeed(0.7f));
            C(0, "i_ipecac", "Ипекак", "Пули больше, взрываются и отравляют цель на 4 с", Unc, Tag("Взрыв + яд", "радиус 3"), Rate(1.5f), BSpeed(0.7f));
            C(0, "i_cookbook", "Поваренная книга анархиста", "Пули — бомбы: взрыв радиусом 3 при попадании, перезарядка дольше", Unc, Tag("Взрыв", "радиус 3"), Reload(1.3f));
            C(0, "i_firemind", "Огненный разум", "Пули поджигают цель: урон от огня 3 с", Common, Tag("Поджог", "3 с"), Dmg(1.1f));
            C(0, "i_polyphemus", "Полифем", "Гигантская пуля: больше размер (x1.6) и урон, но стреляешь реже", Unc, Tag("Гигантская пуля", "x1.6 размер"), Rate(1.6f), BSpeed(0.9f));
            C(0, "i_spoonbender", "Гнутая ложка", "Пули слегка сами ищут врага", Unc, Tag("Самонаведение", "слабое"), Dmg(0.9f));
            C(0, "i_ludovico", "Техника Людовико", "Пули сильно изгибают траекторию к врагу и скачут от стен", Rare, Tag("Самонаведение", "сильное"), Bounce(3), Dmg(0.8f));
            C(0, "i_sacredheart", "Священное сердце", "Наведение пуль, больше урона и здоровья", Rare, Tag("Самонаведение", "среднее"), Hp(1.3f), Dmg(1.3f));
            C(0, "i_mutantspider", "Мутант-паук", "Четыре пули за выстрел, каждая слабее", Rare, Bullets(3), Dmg(0.7f), Rate(1.3f));
            C(0, "i_innereye", "Внутренний глаз", "Три пули за выстрел, но стреляешь реже и разброс шире", Unc, Bullets(2), Rate(1.3f), Spread(1.4f));
            C(0, "i_cricket", "Голова сверчка", "Больше урона и чуть быстрее стрельба", Common, Dmg(1.4f), Rate(0.9f));
            C(0, "i_numberone", "Номер один", "Стреляешь часто, но слабо", Common, Rate(0.65f), Dmg(0.8f));
            C(0, "i_pentagram", "Пентаграмма", "Прибавка к урону", Common, Dmg(1.3f));
            C(0, "i_mark", "Метка", "Немного больше урона, здоровья и скорости", Unc, Dmg(1.1f), Hp(1.2f), Speed(1.1f));
            C(0, "i_rubbercement", "Резиновый клей", "Много отскоков от стен, слабее урон", Common, Bounce(4), Dmg(0.85f));
            C(0, "i_coal", "Кусок угля", "Горячие пули бьют сильнее, но летят медленнее", Common, Dmg(1.35f), BSpeed(0.85f));
            C(0, "i_soymilk", "Соевое молоко", "Очень быстрая стрельба, но слёзы слабые", Unc, Rate(0.4f), Dmg(0.5f));
            C(0, "i_pyro", "Пиро", "Больше патронов — бомбы про запас", Common, Ammo(5), Dmg(0.9f));
            C(0, "i_contacts", "Линзы мамы", "Слёзы замораживают: цель сильно замедляется", Common, Slow(0.25f), Dmg(1.05f));
            C(0, "i_toughlove", "Жёсткая любовь", "Бьёшь сильнее и отбрасываешь далеко", Common, Knock(2f), Dmg(1.2f));
            C(0, "i_momsbra", "Бюстгальтер мамы", "Пули замораживают и отбрасывают", Common, Slow(0.3f), Knock(1.3f));
            // пассивки на выживание
            C(0, "i_mushroom", "Волшебный гриб", "Всё понемногу больше: урон, здоровье, скорость", Rare, Dmg(1.2f), Hp(1.2f), Speed(1.1f));
            C(0, "i_experimental", "Экспериментальное лечение", "Резко больше здоровья и скорости, но урон ниже", Unc, Hp(1.3f), Speed(1.2f), Dmg(0.85f));
            C(0, "i_holymantle", "Священная мантия", "Поглощает первый сильный удар каждого раунда", Rare, Tag("Щит от удара", "1 за раунд"), Hp(1.1f));
            C(0, "i_deadcat", "Мёртвый кот", "Девять жизней: два воскрешения, но почти нет здоровья", Rare, Respawn(2), Hp(0.5f));
            C(0, "i_1up", "1UP", "Дополнительная жизнь и лёгкая регенерация", Rare, Respawn(1), Regen(0.3f));
            C(0, "i_lordpit", "Повелитель ямы", "Полёт: лишние прыжки и малая гравитация, хрупкий", Unc, Jumps(3), Grav(0.7f), Hp(0.8f));
            C(0, "i_transcend", "Трансцендентность", "Лёгкий и быстрый, как призрак", Unc, Grav(0.4f), Speed(1.2f), Jumps(1));
            C(0, "i_meat", "Мясо!", "Прибавка к здоровью и немного урона", Common, Hp(1.35f), Dmg(1.1f));
            C(0, "i_blackrune", "Чёрная руна", "Огромная сила за счёт здоровья", Rare, Dmg(1.6f), Hp(0.7f));
            // орбитали
            C(0, "i_cubemeat", "Куб мяса", "Кусок мяса кружит вокруг тебя и бьёт врагов (14 урона)", Common, Tag("Орбиталь", "мясо x1"), Hp(1.1f));
            C(0, "i_bandages", "Шарик из бинтов", "Два шара из бинтов кружат вокруг, бьют врагов (9 урона)", Unc, Tag("Орбитали", "бинты x2"), Hp(1.05f));
            C(0, "i_dagger", "Жертвенный кинжал", "Быстрый кинжал по широкой орбите (18 урона)", Unc, Tag("Орбиталь", "кинжал x1"), Speed(1.05f));
            // активки — клавиша Q (геймпад: Y)
            C(0, "i_necronomicon", "Некрономикон", "АКТИВКА: 20 урона всем врагам сразу. Перезарядка 20 с", Unc, Tag("Активка [Q]", "20 с"), Ammo(2));
            C(0, "i_belial", "Книга Велиала", "АКТИВКА: на 6 секунд урон пуль x1.8. Перезарядка 18 с", Unc, Tag("Активка [Q]", "18 с"), Dmg(1.1f));
            C(0, "i_yumheart", "Вкусное сердце", "АКТИВКА: лечит 40 здоровья. Перезарядка 15 с", Common, Tag("Активка [Q]", "15 с"), Hp(1.1f));
            C(0, "i_shadows", "Книга теней", "АКТИВКА: 5 секунд неуязвимости. Перезарядка 25 с", Rare, Tag("Активка [Q]", "25 с"), Speed(1.05f));
            C(0, "i_bible", "Библия", "АКТИВКА: святая волна отбрасывает всех врагов и бьёт на 8. Перезарядка 14 с", Unc, Tag("Активка [Q]", "14 с"), Respawn(1));
            C(0, "i_teleport", "Телепорт!", "АКТИВКА: рывок на 7 клеток вперёд (до стены). Перезарядка 8 с", Common, Tag("Активка [Q]", "8 с"), Speed(1.1f));
            C(0, "i_megablast", "Мега-взрыв", "АКТИВКА: 2 секунды гигантский лазер по прицелу (6 импульсов по 22). Перезарядка 35 с", Rare, Tag("Активка [Q]", "35 с"), Dmg(1.1f));
            C(0, "i_shoop", "Шуп-да-Вуп!", "АКТИВКА: один мощный лазер на 70 урона. Перезарядка 20 с", Unc, Tag("Активка [Q]", "20 с"), Reload(0.9f));
            C(0, "i_hourglass", "Песочные часы", "АКТИВКА: все враги сильно замедляются на несколько секунд. Перезарядка 22 с", Unc, Tag("Активка [Q]", "22 с"), Rate(0.9f));
            C(0, "i_tammy", "Голова Тэмми", "АКТИВКА: залп восьми пуль во все стороны. Перезарядка 12 с", Unc, Tag("Активка [Q]", "12 с"), Ammo(2));
            C(0, "i_howtojump", "Как прыгать", "АКТИВКА: мощный прыжок вверх. Перезарядка 6 с", Common, Tag("Активка [Q]", "6 с"), Jump(1.2f));
            C(0, "i_monstermanual", "Монстр-книга", "АКТИВКА: на 12 секунд три орбитали-фамильяра. Перезарядка 24 с", Unc, Tag("Активка [Q]", "24 с"), Hp(1.1f));
            C(0, "i_kamikaze", "Камикадзе", "АКТИВКА: взрыв до 60 урона в радиусе 8, тебе 25. Перезарядка 25 с", Rare, Tag("Активка [Q]", "25 с"), Hp(1.1f));

            // --- S.T.A.L.K.E.R.: артефакты Зоны, адаптированные под ROUNDS ---
            C(0, "z_medusa", "Медуза", "Мягче удары, чуть больше здоровья", Common, TakeOver(1f), Hp(1.1f));
            C(0, "z_stoneflower", "Каменный цветок", "Крепче, но чуть медленнее", Common, Hp(1.25f), Speed(0.95f));
            C(0, "z_nightstar", "Ночная звезда", "Урон и меткость выше, пули быстрее", Unc, Dmg(1.3f), Spread(0.8f), BSpeed(1.1f));
            C(0, "z_thorn", "Колючка", "Больше урона, пули отскакивают", Common, Dmg(1.2f), Bounce(1));
            C(0, "z_soul", "Душа", "Сильная регенерация, чуть медленнее", Unc, Regen(1.2f), Speed(0.95f));
            C(0, "z_vyvert", "Выверт", "Выше прыжок, лишний прыжок и меньше гравитация", Unc, Jump(1.3f), Jumps(1), Grav(0.85f));
            C(0, "z_drop", "Капля", "Лечит от попаданий и понемногу сама", Common, Steal(0.1f), Regen(0.4f));
            C(0, "z_fireball", "Огненный шар", "Горячие пули: сильный урон, чуть быстрее бег", Unc, Dmg(1.4f), Speed(1.05f));
            C(0, "z_goldfish", "Золотая рыбка", "Удача Зоны: патроны, урон и здоровье понемногу", Unc, Ammo(2), Dmg(1.15f), Hp(1.1f));
            C(0, "z_sparkler", "Бенгальский огонь", "Искры: стрельба чаще и бег быстрее", Common, Rate(0.8f), Speed(1.1f), Dmg(0.95f));
            C(0, "z_bubble", "Пузырь", "Упругая защита: удары мягче", Common, Hp(1.2f), TakeOver(1.5f));
            C(0, "z_urchin", "Морской ёж", "Колючие пули: отбрасывание и отскок", Common, Knock(1.5f), Bounce(1));
            C(0, "z_crystal", "Кристалл", "Огранённые пули: точнее и больнее", Common, Spread(0.7f), Dmg(1.2f));
            C(0, "z_kolobok", "Колобок", "Быстрый и маленький", Common, Speed(1.25f), Size(0.9f));
            C(0, "z_beads", "Мамины бусы", "Больше здоровья, немного слабее урон", Common, Hp(1.3f), Dmg(0.95f));
            C(0, "z_stoneblood", "Кровь камня", "Много здоровья и регенерация, бег медленнее", Unc, Hp(1.5f), Regen(0.5f), Speed(0.9f));
            C(0, "z_slime", "Слизь", "Липкие пули: цель замедляется, ты чуть крепче", Common, Slow(0.35f), Hp(1.1f), Speed(0.9f));
            C(0, "z_blank", "Пустышка", "Мелкая прибавка к здоровью и патронам", Common, Hp(1.1f), Ammo(1));
            C(0, "z_battery", "Батарейка", "Патроны сами пополняются, перезарядка быстрее", Unc, AmmoReg(0.5f), Reload(0.8f));
            C(0, "z_flash", "Вспышка", "Быстрая и сильная пуля, перезарядка дольше", Unc, Dmg(1.25f), BSpeed(1.3f), Reload(1.1f));
            C(0, "z_moonlight", "Лунный свет", "Лёгкий шаг: быстрее, выше, мягче падаешь", Unc, Speed(1.2f), Jump(1.2f), Grav(0.9f));
            C(0, "z_gravi", "Грави", "Почти невесомость, но хрупко", Unc, Grav(0.7f), Jump(1.3f), Hp(0.9f));
            C(0, "z_snowflake", "Снежинка", "Холодные пули замедляют цель и летят точнее", Common, Slow(0.3f), Spread(0.9f));
            C(0, "z_spring", "Пружина", "Очень высокий прыжок и лишний прыжок", Common, Jump(1.5f), Jumps(1), Dmg(0.9f));
            C(0, "z_meat", "Ломоть мяса", "Много здоровья и быстрое заживление", Unc, Hp(1.4f), Regen(0.8f));
            C(0, "z_compass", "Компас", "Идеальная точность и быстрые пули", Common, Spread(0.6f), BSpeed(1.25f));
            C(0, "z_oasis", "Сердце оазиса", "Мощная регенерация и больше здоровья", Rare, Regen(1.5f), Hp(1.2f));
            C(0, "z_crystalthorn", "Кристальная колючка", "Острые пули: урон выше, разброс меньше", Unc, Dmg(1.3f), Spread(0.7f));
        }
    }
}
