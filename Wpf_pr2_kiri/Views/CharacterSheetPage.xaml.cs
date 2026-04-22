using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace Wpf_pr2_kiri.Views
{
    /// <summary>
    /// Interaction logic for CharacterSheetPage.xaml
    /// </summary>
    public partial class CharacterSheetPage : Page
    {
        public CharacterSheetPage()
        {
            InitializeComponent();
            UpdateProficiency();
            LoadData();
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) // 1. Заборона введення всього, крім цифр
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)// 2. Очищення поля при фокусі (для зручності)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
        }
        private void Name_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCharName.Text))
                TxtCharName.SetResourceReference(TextBox.TextProperty, "m_CharNamePlaceholder");
        }

        private void Race_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCharRaceClass.Text))
                TxtCharRaceClass.SetResourceReference(TextBox.TextProperty, "m_CharRacePlaceholder");
        }

        private void Level_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtLevel.Text)) TxtLevel.Text = "1";
            UpdateProficiency();
        }

        private void HpChange_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtHpChange.Text)) TxtHpChange.Text = "0";
        }

        private void Number_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(tb.Text)) tb.Text = "0";
        }
        private void UpdateProficiency()
        {
            if (TxtLevel == null || LblProfBonus == null) return;

            if (int.TryParse(TxtLevel.Text, out int level))
            {
                int prof = 2 + (level - 1) / 4;
                LblProfBonus.Text = "+" + prof.ToString();
            }
        }

        private void TxtLevel_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateProficiency();
        }

        private void BtnHeal_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtHpChange.Text, out int change) &&
                int.TryParse(TxtCurrentHp.Text, out int current) &&
                int.TryParse(TxtMaxHp.Text, out int max))
            {
                int newHp = current + change;
                TxtCurrentHp.Text = (newHp > max ? max : newHp).ToString();
            }
        }

        private void BtnDamage_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtHpChange.Text, out int change) &&
                 int.TryParse(TxtCurrentHp.Text, out int current))
            {
                int newHp = current - change;
                TxtCurrentHp.Text = (newHp < 0 ? 0 : newHp).ToString();
            }
        }
        private void CloseRoll_Click(object sender, RoutedEventArgs e)
        {
            // Це приховає вікно з результатом кидка кубика
            RollOverlay.Visibility = Visibility.Collapsed;
        }

        private int GetMod(string text)
        {
            if (int.TryParse(text, out int val))
                return (int)Math.Floor((val - 10) / 2.0);
            return 0;
        }

        // Форматування тексту (додає "+" до позитивних чисел)
        private string ModStr(int mod) => (mod >= 0 ? "+" : "") + mod;

        // Головна подія при зміні цифр у будь-якому статі
        private void Stat_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Перевірка на ініціалізацію (щоб програма не вилетіла при старті)
            if (TxtModStr == null || TxtModDex == null || TxtModCon == null ||
                TxtModInt == null || TxtModWis == null || TxtModCha == null) return;

            // Оновлюємо текст у кружечках
            TxtModStr.Text = ModStr(GetMod(TxtStatStr.Text));
            TxtModDex.Text = ModStr(GetMod(TxtStatDex.Text));
            TxtModCon.Text = ModStr(GetMod(TxtStatCon.Text));
            TxtModInt.Text = ModStr(GetMod(TxtStatInt.Text));
            TxtModWis.Text = ModStr(GetMod(TxtStatWis.Text));
            TxtModCha.Text = ModStr(GetMod(TxtStatCha.Text));

            // Оновлюємо AC та Ініціативу, бо вони залежать від статів
            UpdateAC(this, EventArgs.Empty);
            UpdateIni(this, EventArgs.Empty);
        }

        // Оновлення Захисту (AC)
        private void UpdateAC(object sender, EventArgs e)
        {
            if (LblTotalAC == null || TxtBaseArmor == null || ComboACMod == null) return;

            int baseAC = int.TryParse(TxtBaseArmor.Text, out int b) ? b : 10;
            int bonus = 0;

            if (ComboACMod.SelectedIndex == 1) bonus = GetMod(TxtStatDex.Text);
            else if (ComboACMod.SelectedIndex == 2) bonus = GetMod(TxtStatCon.Text);

            LblTotalAC.Text = (baseAC + bonus).ToString();
        }

        // Оновлення Ініціативи
        private void UpdateIni(object sender, EventArgs e)
        {
            if (LblTotalIni == null || TxtBonusIni == null) return;

            int dexMod = GetMod(TxtStatDex.Text);
            int extraBonus = int.TryParse(TxtBonusIni.Text, out int b) ? b : 0;

            LblTotalIni.Text = ModStr(dexMod + extraBonus);
        }
        private void TxtCharRaceClass_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            SaveData();
        }

        private void BtnRollStat_Click(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn == null) return;

            // Визначаємо який стат кидаємо по імені кнопки (напр. BtnRollStr -> Str)
            string statName = btn.Name.Replace("BtnRoll", "");
            TextBox targetTxt = (TextBox)this.FindName("TxtStat" + statName);

            if (targetTxt == null) return;

            int mod = GetMod(targetTxt.Text);
            Random rnd = new Random();
            int roll = rnd.Next(1, 21); // d20

            // Заповнюємо спливаюче вікно (Overlay)
            TxtRollTitle.Text = statName.ToUpper();
            TxtRollResult.Text = (roll + mod).ToString();
            TxtRollDetails.Text = $"{roll} (d20) {ModStr(mod)}";

            // Показуємо вікно
            RollOverlay.Visibility = Visibility.Visible;
        }
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
            NavigationService.GoBack();
        }
        public class CharacterData
        {
            // Основна інформація
            public string? CharacterName { get; set; }
            public string? CharacterClass { get; set; }
            public string? Level { get; set; }

            // Характеристики (Сили, Спритність і т.д.)
            public string? Strength { get; set; }
            public string? Dexterity { get; set; }
            public string? Constitution { get; set; }
            public string? Intelligence { get; set; }
            public string? Wisdom { get; set; }
            public string? Charisma { get; set; }
            // Параметри бою
            public string? CurrentHP { get; set; }
            public string? MaxHP { get; set; }
            public string? ArmorClass { get; set; }
            public string? Speed { get; set; }
            public string? Initiative { get; set; }
            public string? ProficiencyBonus { get; set; }

            // Біографія
            public string? Backstory { get; set; }
            public string? AlliesNotes { get; set; }
        }
        private void SaveData()
        {
            try
            {
                var data = new CharacterData
                {
                    CharacterName = TxtCharName.Text,
                    // Оновлено: тепер беремо дані з TxtCharRaceClass
                    CharacterClass = TxtCharRaceClass.Text,
                    Level = TxtLevel.Text,

                    Strength = TxtStatStr.Text,
                    Dexterity = TxtStatDex.Text,
                    Constitution = TxtStatCon.Text,
                    Intelligence = TxtStatInt.Text,
                    Wisdom = TxtStatWis.Text,
                    Charisma = TxtStatCha.Text,

                    CurrentHP = TxtCurrentHp.Text,
                    MaxHP = TxtMaxHp.Text,
                    ArmorClass = TxtBaseArmor.Text,
                    Speed = TxtSpeed.Text,
                    Initiative = TxtBonusIni.Text,
                    ProficiencyBonus = LblProfBonus.Text,

                    Backstory = TxtBackstory.Text,
                    AlliesNotes = TxtAlliesNotes.Text
                };

                string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("character_save.json", jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при збереженні: " + ex.Message);
            }
        }
        private void LoadData()
        {
            string filePath = "character_save.json";

            // Перевіряємо, чи існує файл взагалі
            if (File.Exists(filePath))
            {
                try
                {
                    // 1. Читаємо текст із файлу
                    string jsonString = File.ReadAllText(filePath);

                    // 2. Створюємо об'єкт 'data', десеріалізуючи JSON
                    var data = JsonSerializer.Deserialize<CharacterData>(jsonString);

                    // 3. Тепер 'data' існує, і ми можемо заповнювати поля
                    if (data != null)
                    {
                        TxtCharName.Text = data.CharacterName;
                        TxtCharRaceClass.Text = data.CharacterClass; // Клас/Раса
                        TxtLevel.Text = data.Level;

                        TxtStatStr.Text = data.Strength;
                        TxtStatDex.Text = data.Dexterity;
                        TxtStatCon.Text = data.Constitution;
                        TxtStatInt.Text = data.Intelligence;
                        TxtStatWis.Text = data.Wisdom;
                        TxtStatCha.Text = data.Charisma;

                        TxtCurrentHp.Text = data.CurrentHP;
                        TxtMaxHp.Text = data.MaxHP;
                        TxtBaseArmor.Text = data.ArmorClass;
                        TxtSpeed.Text = data.Speed;
                        TxtBonusIni.Text = data.Initiative;
                        LblProfBonus.Text = data.ProficiencyBonus; // Для Label використовуємо Content

                        TxtBackstory.Text = data.Backstory;
                        TxtAlliesNotes.Text = data.AlliesNotes;
                    }
                }
                catch (Exception ex)
                {
                    // Якщо файл пошкоджений або помилка читання
                    System.Windows.MessageBox.Show("Помилка завантаження: " + ex.Message);
                }
            }
        }
    }
}
