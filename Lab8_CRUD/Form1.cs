using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Lab8_CRUD
{
    public partial class Form1 : Form
    {
        string connStr = @"Server=ALEKSUSER;Database=StudentDB;Integrated Security=True;";

        DataView view;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            dataGridViewStudents.SelectionChanged += dataGridViewStudents_SelectionChanged;
            buttonAdd.Click += buttonAdd_Click;
            buttonUpdate.Click += buttonUpdate_Click;
            buttonDelete.Click += buttonDelete_Click;
            buttonClear.Click += buttonClear_Click;
            buttonExport.Click += buttonExport_Click;
            buttonCancel.Click += buttonCancel_Click;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadStudents();
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            string name = textBoxName.Text.Trim();
            string group = textBoxGroup.Text.Trim();

            // валидация
            if (name == "")
            {
                MessageBox.Show("Введите ФИО.");
                return;
            }
            if (group == "")
            {
                MessageBox.Show("Введите группу.");
                return;
            }

            try
            {
                SqlConnection conn = new SqlConnection(connStr);
                conn.Open();
                SqlCommand check = new SqlCommand(
                    "SELECT COUNT(*) FROM Students WHERE FullName = @name", conn);
                check.Parameters.AddWithValue("@name", name);
                int count = Convert.ToInt32(check.ExecuteScalar());

                if (count > 0)
                {
                    MessageBox.Show("Студент с таким ФИО уже есть в базе.");
                    conn.Close();
                    return;
                }

                // INSERT с параметрами
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Students (FullName, Age, GroupName, AverageGrade) " +
                    "VALUES (@name, @age, @group, @grade)", conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@age", (int)numericAge.Value);
                cmd.Parameters.AddWithValue("@group", group);
                cmd.Parameters.AddWithValue("@grade", (double)numericGrade.Value);

                cmd.ExecuteNonQuery();
                conn.Close();

                LoadStudents();
                ClearFields();
                MessageBox.Show("Студент добавлен.");
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Ошибка добавления: " + ex.Message);
            }
        }

        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            if (dataGridViewStudents.CurrentRow == null)
            {
                MessageBox.Show("Выберите студента в таблице.");
                return;
            }

            string name = textBoxName.Text.Trim();
            string group = textBoxGroup.Text.Trim();

            if (name == "" || group == "")
            {
                MessageBox.Show("Заполните ФИО и группу.");
                return;
            }

            int id = Convert.ToInt32(dataGridViewStudents.CurrentRow.Cells["Id"].Value);

            try
            {
                SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "UPDATE Students SET FullName=@name, Age=@age, " +
                    "GroupName=@group, AverageGrade=@grade WHERE Id=@id", conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@age", (int)numericAge.Value);
                cmd.Parameters.AddWithValue("@group", group);
                cmd.Parameters.AddWithValue("@grade", (double)numericGrade.Value);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
                conn.Close();

                LoadStudents();
                ClearFields();
                MessageBox.Show("Данные обновлены.");
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Ошибка обновления: " + ex.Message);
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            ClearFields();
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (dataGridViewStudents.CurrentRow == null)
            {
                MessageBox.Show("Выберите студента в таблице.");
                return;
            }

            string name = dataGridViewStudents.CurrentRow.Cells["FullName"].Value.ToString();
            int id = Convert.ToInt32(dataGridViewStudents.CurrentRow.Cells["Id"].Value);

            DialogResult result = MessageBox.Show(
                "Удалить студента \"" + name + "\"?",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;
            try
            {
                SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                SqlCommand cmd = new SqlCommand("DELETE FROM Students WHERE Id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                conn.Close();

                LoadStudents();
                ClearFields();
                MessageBox.Show("Студент удалён.");
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message);
            }

        }

        private void LoadStudents()
        {
            try
            {
                SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT * FROM Students", conn);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable table = new DataTable();
                adapter.Fill(table);
                conn.Close();

                dataGridViewStudents.DataSource = table;

                if (dataGridViewStudents.Columns["Id"] != null)
                    dataGridViewStudents.Columns["Id"].Visible = false;

                labelStatus.Text = "Всего студентов: " + table.Rows.Count;
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        private void dataGridViewStudents_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewStudents.CurrentRow == null) return;

            DataGridViewRow row = dataGridViewStudents.CurrentRow;
            if (row.Cells["FullName"].Value == null) return;

            textBoxName.Text = row.Cells["FullName"].Value.ToString();
            numericAge.Value = Convert.ToDecimal(row.Cells["Age"].Value);
            textBoxGroup.Text = row.Cells["GroupName"].Value.ToString();
            numericGrade.Value = Convert.ToDecimal(row.Cells["AverageGrade"].Value);
        }

        private void ClearFields()
        {
            textBoxName.Text = "";
            textBoxGroup.Text = "";
            numericAge.Value = 15;
            numericGrade.Value = 0;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            ClearFields();
            LoadStudents();
            MessageBox.Show("Данные перезагружены.");
        }



        private void textBoxSearch_TextChanged(object sender, EventArgs e)
        {
            ApplySearch();
        }

        private void ApplySearch()
        {
            if (view == null) return;

            string text = textBoxSearch.Text.Trim();

            if (text == "")
            {
                view.RowFilter = "";
            }
            else
            {
                view.RowFilter = "FullName LIKE '%" + text + "%'";
            }
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            if (dataGridViewStudents.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.");
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "CSV-файл (*.csv)|*.csv";
            dialog.FileName = "students.csv";

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                StringBuilder sb = new StringBuilder();

                // заголовок
                sb.AppendLine("Id;FullName;Age;GroupName;AverageGrade");

                // строки
                foreach (DataGridViewRow row in dataGridViewStudents.Rows)
                {
                    if (row.IsNewRow) continue;

                    string id = row.Cells["Id"].Value.ToString();
                    string name = row.Cells["FullName"].Value.ToString();
                    string age = row.Cells["Age"].Value.ToString();
                    string group = row.Cells["GroupName"].Value.ToString();
                    string grade = row.Cells["AverageGrade"].Value.ToString();

                    sb.AppendLine(id + ";" + name + ";" + age + ";" + group + ";" + grade);
                }

                // пишем в кодировке UTF-8 (чтобы кириллица отображалась в Excel)
                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);

                MessageBox.Show("Сохранено в: " + dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта: " + ex.Message);
            }
        }
    }
}
