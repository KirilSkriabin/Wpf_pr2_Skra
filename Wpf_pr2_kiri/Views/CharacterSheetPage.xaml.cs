using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Wpf_pr2_kiri.Views
{
    // Клас-модель для однієї атаки або заклинання
    public class AttackItem
    {
        public string? Name { get; set; }
        public string? Level { get; set; }
        public string? CastingTime { get; set; }
        public string? Range { get; set; }
        public string? Duration { get; set; }
        public string? Components { get; set; }
        public string? Damage { get; set; }
        public string? Description { get; set; }
    }

    // Спрощена модель предмету з підтримкою INotifyPropertyChanged
    public class ItemData : INotifyPropertyChanged
    {
        public string? Name { get; set; }
        public int Qty { get; set; } = 1;
        public string? Description { get; set; }

        private string? _uses;
        public string? Uses
        {
            get => _uses;
            set
            {
                if (_uses != value)
                {
                    _uses = value;
                    OnPropertyChanged(nameof(Uses));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Модель одного запису в історії кидків (не зберігається в JSON)
    public class RollEntry
    {
        public int Index { get; set; }
        public string Title { get; set; } = "";
        public string Details { get; set; } = "";
        public string Result { get; set; } = "";
        public string ResultColor { get; set; } = "#2b5c8f";
    }

    public partial class CharacterSheetPage : Page
    {
        // =======================================================
        // ВИКОРИСТАННЯ 4-Х СКЛАДЕНИХ СТРУКТУР ДАНИХ
        // =======================================================

        // 1. LIST (Список): Динамічна структура для збереження атак/заклинань
        private List<AttackItem> _attacksList = new List<AttackItem>();

        // LIST для предметів
        private List<ItemData> _itemsList = new List<ItemData>();

        // 2. STACK (Стек): Тимчасова історія кидків (LIFO - Останній прийшов, першим пішов)
        private Stack<string> _rollHistory = new Stack<string>();

        // 3. DICTIONARY (Словник): Швидке співставлення системних назв із затишними українськими
        private Dictionary<string, string> _statNamesUa = new Dictionary<string, string>()
        {
            { "Str", "Сила" }, { "Dex", "Спритність" }, { "Con", "Статура" },
            { "Int", "Інтелект" }, { "Wis", "Мудрість" }, { "Cha", "Харизма" }
        };

        // 4. ARRAY (Масив): Фиксований список системних назв характеристик
        private readonly string[] _coreStatsArray = new string[] { "Str", "Dex", "Con", "Int", "Wis", "Cha" };

        // Тимчасова історія кидків для вкладки History (не зберігається в JSON)
        private ObservableCollection<RollEntry> _rollLog = new ObservableCollection<RollEntry>();

        public CharacterSheetPage()
        {
            InitializeComponent();
            UpdateProficiency();
            LoadData();
            RollHistoryControl.ItemsSource = _rollLog;
        }

        // =======================================================
        // ДОПОМІЖНИЙ МЕТОД: ДОДАТИ ЗАПИС В ІСТОРІЮ
        // =======================================================
        private void AddRollToHistory(string title, string details, int result)
        {
            string color;
            if (result >= 20) color = "#C62828"; // натуральна 20 або дуже добре — червоний
            else if (result >= 15) color = "#2E7D32"; // добре — зелений
            else if (result >= 10) color = "#1565C0"; // середнє — синій
            else if (result >= 5) color = "#F57F17"; // погано — жовтий
            else color = "#4A148C"; // дуже погано — фіолетовий

            var entry = new RollEntry
            {
                Index = _rollLog.Count + 1,
                Title = title,
                Details = details,
                Result = result.ToString(),
                ResultColor = color
            };

            _rollLog.Add(entry);

            // Автоматично гортаємо вниз до останнього кидка
            HistoryScrollViewer?.ScrollToBottom();
        }

        // Очистити історію кидків
        private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _rollLog.Clear();
        }

        // Заборона введення всього, крім цифр
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Очищення поля при фокусі (для зручності)
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
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

        // Подія закриття вікна кидка (Використовуємо STACK)
        private void CloseRoll_Click(object sender, RoutedEventArgs e)
        {
            if (_rollHistory.Count > 0)
            {
                string lastFinishedRoll = _rollHistory.Pop();
            }

            RollOverlay.Visibility = Visibility.Collapsed;
        }

        private int GetMod(string text)
        {
            if (int.TryParse(text, out int val))
                return (int)Math.Floor((val - 10) / 2.0);
            return 0;
        }

        private string ModStr(int mod) => (mod >= 0 ? "+" : "") + mod;

        private void Stat_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtModStr == null || TxtModDex == null || TxtModCon == null ||
                TxtModInt == null || TxtModWis == null || TxtModCha == null) return;

            TxtModStr.Text = ModStr(GetMod(TxtStatStr.Text));
            TxtModDex.Text = ModStr(GetMod(TxtStatDex.Text));
            TxtModCon.Text = ModStr(GetMod(TxtStatCon.Text));
            TxtModInt.Text = ModStr(GetMod(TxtStatInt.Text));
            TxtModWis.Text = ModStr(GetMod(TxtStatWis.Text));
            TxtModCha.Text = ModStr(GetMod(TxtStatCha.Text));

            UpdateAC(this, EventArgs.Empty);
            UpdateIni(this, EventArgs.Empty);
        }

        private void UpdateAC(object sender, EventArgs e)
        {
            if (LblTotalAC == null || TxtBaseArmor == null || ComboACMod == null) return;

            int baseAC = int.TryParse(TxtBaseArmor.Text, out int b) ? b : 10;
            int bonus = 0;

            if (ComboACMod.SelectedIndex == 1) bonus = GetMod(TxtStatDex.Text);
            else if (ComboACMod.SelectedIndex == 2) bonus = GetMod(TxtStatCon.Text);

            LblTotalAC.Text = (baseAC + bonus).ToString();
        }

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

        // Кнопка кидка кубика d20 для характеристик
        private void BtnRollStat_Click(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn == null) return;

            string statName = btn.Name.Replace("BtnRoll", "");
            TextBox targetTxt = (TextBox)this.FindName("TxtStat" + statName);

            if (targetTxt == null) return;

            int mod = GetMod(targetTxt.Text);
            Random rnd = new Random();
            int roll = rnd.Next(1, 21);
            int total = roll + mod;

            string titleUa = _statNamesUa.ContainsKey(statName) ? _statNamesUa[statName] : statName;

            TxtRollTitle.Text = titleUa.ToUpper();
            TxtRollResult.Text = total.ToString();
            TxtRollDetails.Text = $"{roll} (d20) {ModStr(mod)}";

            _rollHistory.Push($"{statName} roll: {total}");

            // Додаємо в історію
            AddRollToHistory(
                titleUa.ToUpper(),
                $"d20 = {roll}  {ModStr(mod)} (mod)",
                total
            );

            RollOverlay.Visibility = Visibility.Visible;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
            NavigationService.GoBack();
        }

        // =======================================================
        // ЛОГІКА ДЛЯ ВКЛАДКИ АТАК
        // =======================================================

        private void BtnOpenAddForm_Click(object sender, RoutedEventArgs e)
        {
            TxtAttackName.Text = string.Empty;
            TxtCastingTime.Text = "n/a";
            TxtRange.Text = "n/a";
            TxtDuration.Text = "n/a";
            TxtDamage.Text = "n/a";
            TxtDescription.Text = string.Empty;
            ChkV.IsChecked = false;
            ChkS.IsChecked = false;
            ChkM.IsChecked = false;
            if (TxtMaterialComponent != null) TxtMaterialComponent.Text = string.Empty;

            AttackCreationForm.Visibility = Visibility.Visible;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            AttackCreationForm.Visibility = Visibility.Collapsed;
        }

        private void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtAttackName.Text))
            {
                MessageBox.Show("Введіть обов'язкове поле: Назва атаки!", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<string> components = new List<string>();
            if (ChkV.IsChecked == true) components.Add("V");
            if (ChkS.IsChecked == true) components.Add("S");
            if (ChkM.IsChecked == true) components.Add("M");
            string compResult = components.Count > 0 ? string.Join(", ", components) : "n/a";

            if (TxtMaterialComponent != null && !string.IsNullOrWhiteSpace(TxtMaterialComponent.Text) && ChkM.IsChecked == true)
            {
                compResult += $" ({TxtMaterialComponent.Text})";
            }

            AttackItem newAttack = new AttackItem()
            {
                Name = TxtAttackName.Text,
                Level = (CmbAttackLevel.SelectedItem as ComboBoxItem)?.Content.ToString(),
                CastingTime = TxtCastingTime.Text,
                Range = TxtRange.Text,
                Duration = TxtDuration.Text,
                Components = compResult,
                Damage = TxtDamage.Text,
                Description = TxtDescription.Text
            };

            _attacksList.Add(newAttack);
            RefreshAttacksList();
            AttackCreationForm.Visibility = Visibility.Collapsed;
            SaveData();
        }

        private void BtnDeleteAttack_Click(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn == null) return;

            AttackItem? attackToDelete = btn.DataContext as AttackItem;
            if (attackToDelete == null) return;

            var result = MessageBox.Show($"Ви впевнені, що хочете видалити атакy/заклинання \"{attackToDelete.Name}\"?",
                                         "Підтвердження видалення",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _attacksList.Remove(attackToDelete);
                RefreshAttacksList();
                SaveData();
            }
        }

        // МАТЕМАТИЧНИЙ КИДОК КУБИКІВ ШКОДИ
        private int ParseAndRollDice(string diceExpression, out string details)
        {
            details = "0";
            string clean = diceExpression.Replace(" ", "").ToLower();

            var match = Regex.Match(clean, @"^(\d+)d(\d+)(?:([+-]\d+))?$");

            if (!match.Success)
            {
                if (int.TryParse(clean, out int flatDamage))
                {
                    details = flatDamage.ToString();
                    return flatDamage;
                }
                return 0;
            }

            int count = int.Parse(match.Groups[1].Value);
            int sides = int.Parse(match.Groups[2].Value);
            int modifier = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

            Random rnd = new Random();
            List<int> individualRolls = new List<int>();
            int sum = 0;

            for (int i = 0; i < count; i++)
            {
                int roll = rnd.Next(1, sides + 1);
                individualRolls.Add(roll);
                sum += roll;
            }

            sum += modifier;

            string rollsJoined = string.Join(" + ", individualRolls);
            details = count > 1 ? $"({rollsJoined})" : rollsJoined;
            if (modifier != 0)
                details += $" {(modifier > 0 ? "+" : "")}{modifier}";

            return sum;
        }

        private void BtnRollDamage_Click(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn == null) return;

            AttackItem? attack = btn.DataContext as AttackItem;
            if (attack == null || string.IsNullOrWhiteSpace(attack.Damage) || attack.Damage.ToLower() == "n/a")
            {
                MessageBox.Show("Для цієї атаки не вказано коректну формулу кубиків ушкодження!", "Ушкодження відсутнє");
                return;
            }

            int totalDamage = ParseAndRollDice(attack.Damage, out string rollDetails);

            if (TxtRollTitle != null && TxtRollResult != null && TxtRollDetails != null && RollOverlay != null)
            {
                TxtRollTitle.Text = (attack.Name ?? "УШКОДЖЕННЯ").ToUpper();
                TxtRollResult.Text = totalDamage.ToString();
                TxtRollDetails.Text = $"{attack.Damage} ➔ {rollDetails}";

                _rollHistory.Push($"{attack.Name} damage: {totalDamage}");

                // Додаємо в історію
                AddRollToHistory(
                    (attack.Name ?? "DAMAGE").ToUpper(),
                    $"{attack.Damage}  ➔  {rollDetails}",
                    totalDamage
                );

                RollOverlay.Visibility = Visibility.Visible;
            }
        }

        private void RefreshAttacksList()
        {
            if (AttacksItemsControl == null) return;

            string selected = (CmbAttackFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";

            if (selected == "All")
            {
                AttacksItemsControl.ItemsSource = null;
                AttacksItemsControl.ItemsSource = _attacksList;
            }
            else
            {
                var filtered = _attacksList.FindAll(a => a.Level == selected);
                AttacksItemsControl.ItemsSource = null;
                AttacksItemsControl.ItemsSource = filtered;
            }
        }

        private void CmbAttackFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshAttacksList();
        }

        // =======================================================
        // ЛОГІКА ДЛЯ ВКЛАДКИ ITEMS
        // =======================================================

        private void BtnOpenItemForm_Click(object sender, RoutedEventArgs e)
        {
            TxtItemName.Text = string.Empty;
            TxtItemQty.Text = "1";
            TxtItemUses.Text = string.Empty;
            TxtItemDescription.Text = string.Empty;
            ItemCreationForm.Visibility = Visibility.Visible;
        }

        private void BtnItemCancel_Click(object sender, RoutedEventArgs e)
        {
            ItemCreationForm.Visibility = Visibility.Collapsed;
        }

        private void BtnItemDone_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtItemName.Text))
            {
                MessageBox.Show("Введіть обов'язкове поле: Item Name!", "Увага",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int qty = int.TryParse(TxtItemQty.Text, out int q) ? q : 1;

            var newItem = new ItemData
            {
                Name = TxtItemName.Text,
                Qty = qty,
                Uses = TxtItemUses.Text,
                Description = TxtItemDescription.Text
            };

            _itemsList.Add(newItem);
            RefreshItemsList();
            ItemCreationForm.Visibility = Visibility.Collapsed;
            SaveData();
        }

        private void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn == null) return;

            ItemData? itemToDelete = btn.DataContext as ItemData;
            if (itemToDelete == null) return;

            var result = MessageBox.Show(
                $"Видалити предмет \"{itemToDelete.Name}\"?",
                "Підтвердження видалення",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _itemsList.Remove(itemToDelete);
                RefreshItemsList();
                SaveData();
            }
        }

        private void RefreshItemsList()
        {
            ItemsItemsControl.ItemsSource = null;
            ItemsItemsControl.ItemsSource = _itemsList;
        }

        // Збереження Uses прямо з картки предмету при втраті фокусу
        private void TxtItemUsesInline_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox? tb = sender as TextBox;
            if (tb == null) return;

            ItemData? item = tb.Tag as ItemData;
            if (item == null) return;

            item.Uses = tb.Text;
            SaveData();
        }

        private void Currency_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LblTotalGp == null) return;

            double pp = double.TryParse(TxtPlatinum?.Text, out double v1) ? v1 : 0;
            double gp = double.TryParse(TxtGold?.Text, out double v2) ? v2 : 0;
            double ep = double.TryParse(TxtElectrum?.Text, out double v3) ? v3 : 0;
            double sp = double.TryParse(TxtSilver?.Text, out double v4) ? v4 : 0;
            double cp = double.TryParse(TxtCopper?.Text, out double v5) ? v5 : 0;

            double total = pp * 10 + gp + ep * 0.5 + sp * 0.1 + cp * 0.01;
            LblTotalGp.Text = $"{total:F2} gp";

            SaveData();
        }

        private void Currency_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(tb.Text))
                tb.Text = "0";
        }

        // =======================================================
        // JSON СЕРИАЛІЗАЦІЯ
        // =======================================================
        public class CharacterData
        {
            public string? CharacterName { get; set; }
            public string? CharacterClass { get; set; }
            public string? Level { get; set; }
            public string? Strength { get; set; }
            public string? Dexterity { get; set; }
            public string? Constitution { get; set; }
            public string? Intelligence { get; set; }
            public string? Wisdom { get; set; }
            public string? Charisma { get; set; }
            public string? CurrentHP { get; set; }
            public string? MaxHP { get; set; }
            public string? ArmorClass { get; set; }
            public string? Speed { get; set; }
            public string? Initiative { get; set; }
            public string? ProficiencyBonus { get; set; }
            public string? Backstory { get; set; }
            public string? AlliesNotes { get; set; }
            public string? Platinum { get; set; }
            public string? Gold { get; set; }
            public string? Electrum { get; set; }
            public string? Silver { get; set; }
            public string? Copper { get; set; }
            public List<AttackItem> Attacks { get; set; } = new List<AttackItem>();
            public List<ItemData> Items { get; set; } = new List<ItemData>();
        }

        private void SaveData()
        {
            try
            {
                var data = new CharacterData
                {
                    CharacterName = TxtCharName.Text,
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
                    AlliesNotes = TxtAlliesNotes.Text,
                    Platinum = TxtPlatinum.Text,
                    Gold = TxtGold.Text,
                    Electrum = TxtElectrum.Text,
                    Silver = TxtSilver.Text,
                    Copper = TxtCopper.Text,
                    Attacks = _attacksList,
                    Items = _itemsList
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

            if (File.Exists(filePath))
            {
                try
                {
                    string jsonString = File.ReadAllText(filePath);
                    var data = JsonSerializer.Deserialize<CharacterData>(jsonString);

                    if (data != null)
                    {
                        TxtCharName.Text = data.CharacterName;
                        TxtCharRaceClass.Text = data.CharacterClass;
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
                        LblProfBonus.Text = data.ProficiencyBonus;
                        TxtBackstory.Text = data.Backstory;
                        TxtAlliesNotes.Text = data.AlliesNotes;
                        TxtPlatinum.Text = data.Platinum ?? "0";
                        TxtGold.Text = data.Gold ?? "0";
                        TxtElectrum.Text = data.Electrum ?? "0";
                        TxtSilver.Text = data.Silver ?? "0";
                        TxtCopper.Text = data.Copper ?? "0";

                        _attacksList = data.Attacks ?? new List<AttackItem>();
                        RefreshAttacksList();

                        _itemsList = data.Items ?? new List<ItemData>();
                        RefreshItemsList();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка завантаження: " + ex.Message);
                }
            }
        }
    }
}