using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb; // Бібліотека для роботи з базами даних OLE DB (включаючи MS Access)

namespace Lab11_OOP
{
    public partial class Form1 : Form
    {
        // Підключення БД. 
        string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=HR_Database.accdb;";

        public Form1()
        {
            InitializeComponent();
        }

        // Універсальний допоміжний метод для виконання SELECT-запитів 
        // та виведення отриманого результату у таблицю DataGridView
        private void LoadDataToGrid(string query)
        {
            try
            {
                // Використовуємо блок using для автоматичного закриття підключення після завершення роботи з БД
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    // OleDbDataAdapter - це "міст" між базою даних та нашою програмою. Він отримує та виконує запит.
                    OleDbDataAdapter adapter = new OleDbDataAdapter(query, connection);

                    // DataTable - це віртуальна таблиця в оперативній пам'яті, куди ми завантажимо дані
                    DataTable table = new DataTable();

                    // Заповнюємо таблицю результатами виконання SQL-запиту
                    adapter.Fill(table);

                    // Прив'язуємо заповнену таблицю до візуального компонента DataGridView на формі
                    dataGridViewReports.DataSource = table;
                }
            }
            catch (Exception ex)
            {
                // Якщо щось піде не так (наприклад, файл БД не знайдено або помилка в SQL), покажемо вікно з помилкою
                MessageBox.Show("Помилка роботи з базою даних: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Введення даних у БД (Додавання нового співробітника)
        private void btnAddEmployee_Click(object sender, EventArgs e)
        {
            // SQL-запит на додавання (INSERT). 
            // @name, @phone, @dept - це параметри. Їх використання захищає базу від SQL-ін'єкцій та помилок синтаксису (наприклад, якщо в імені є апостроф).
            string query = "INSERT INTO Employees (FullName, Phone, DeptID) VALUES (@name, @phone, @dept)";

            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                // Створюємо об'єкт команди, який безпосередньо виконає наш запит у базі
                OleDbCommand command = new OleDbCommand(query, connection);

                // Підставляємо реальні значення з текстових полів (TextBox) на формі замість параметрів у запиті
                command.Parameters.AddWithValue("@name", txtFullName.Text);
                command.Parameters.AddWithValue("@phone", txtPhone.Text);

                // Перетворюємо текст на число (Int32), оскільки поле DeptID у базі має числовий тип
                command.Parameters.AddWithValue("@dept", Convert.ToInt32(txtDeptID.Text));

                try
                {
                    // Відкриваємо фізичне підключення до бази даних
                    connection.Open();

                    // Виконуємо запит, який модифікує дані, але не повертає таблицю (INSERT, UPDATE, DELETE)
                    command.ExecuteNonQuery();

                    MessageBox.Show("Співробітника успішно додано!", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Очищуємо поля введення на формі, щоб підготувати їх для наступного співробітника
                    txtFullName.Clear();
                    txtPhone.Clear();
                    txtDeptID.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка при додаванні: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Побудова Звіту 1 (Список співробітників + Назва їх відділу)
        private void btnReport1_Click(object sender, EventArgs e)
        {
            // Запит з використанням INNER JOIN. 
            // Він об'єднує дві таблиці (Employees та Departments) за їхнім спільним полем (DeptID).
            // Ключове слово AS дозволяє задати красиві назви (псевдоніми) для стовпчиків, які побачить користувач.
            string query = @"SELECT Employees.FullName AS [ПІБ], 
                                    Employees.Phone AS [Телефон], 
                                    Departments.DeptName AS [Назва відділу]
                             FROM Employees 
                             INNER JOIN Departments ON Employees.DeptID = Departments.DeptID";

            // Викликаємо наш універсальний метод для виконання запиту та виведення результату
            LoadDataToGrid(query);
        }

        // Побудова Звіту 2 (Кількість працівників у кожному відділі)
        private void btnReport2_Click(object sender, EventArgs e)
        {
            // Запит з використанням агрегатної функції COUNT (підрахунок) та групування GROUP BY
            string query = @"SELECT Departments.DeptName AS [Відділ], 
                                    COUNT(Employees.EmpID) AS [Кількість працівників]
                             FROM Departments 
                             LEFT JOIN Employees ON Departments.DeptID = Employees.DeptID 
                             GROUP BY Departments.DeptName";

            LoadDataToGrid(query);
        }

        // Пошук у БД по одному критерію (За ПІБ або частиною ПІБ)
        private void btnSearch_Click(object sender, EventArgs e)
        {
            // Використовуємо оператор LIKE для пошуку за шаблоном (не точний збіг, а входження тексту)
            string query = "SELECT * FROM Employees WHERE FullName LIKE @search";

            try
            {
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    OleDbDataAdapter adapter = new OleDbDataAdapter(query, connection);

                    // Підставляємо параметр для пошуку. 
                    // Знак % означає "будь-яка кількість будь-яких символів". 
                    // Тобто, якщо користувач введе "Іван", програма шукатиме "%Іван%" (Іван може бути на початку, в середині або в кінці ПІБ).
                    adapter.SelectCommand.Parameters.AddWithValue("@search", "%" + txtSearch.Text + "%");

                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    // Виводимо знайдені дані у таблицю
                    dataGridViewReports.DataSource = table;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка пошуку: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}