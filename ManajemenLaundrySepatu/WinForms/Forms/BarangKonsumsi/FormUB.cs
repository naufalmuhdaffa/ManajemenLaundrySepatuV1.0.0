﻿using ManajemenLaundrySepatu.Helpers;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ManajemenLaundrySepatu
{
    public partial class FormUB : BaseForm
    {
        private string connectionString = "Data Source=LAPTOP-GIV3RJG5\\NADA;Initial Catalog=manajemenLaundrySepatu;Integrated Security=True";
        private string _origNamaBarang, _origSatuan;
        private int _origJumlah;

        public FormUB()
        {
            InitializeComponent();
        }

        private void FormUB_Load(object sender, EventArgs e)
        {
            LoadDataBarang();

            inputSearchBarang._TextChanged += InputSearchBarang_TextChanged;
        }

        private void LoadDataBarang()
        {
            if (!Session.CekSession()) return;

            string cacheKey = $"Cache:BarangKonsumsi_{Session.LoggedInUserId}";

            if (Cache.HasCache(cacheKey))
            {
                DataTable cached = Cache.GetCache(cacheKey) as DataTable;
                if (cached != null)
                {
                    dgvBarang.DataSource = cached;
                    dgvBarang.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                    DateTime lastUpdate = Cache.GetLastUpdate(cacheKey);
                    FormToast toast = new FormToast($"Data di-load dari cache, \nterakhir update: {lastUpdate}");
                    toast.Show();

                    return;
                }
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_ReadBarangKonsumsi", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id_akun", Session.LoggedInUserId);
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        dgvBarang.DataSource = dt;
                        dgvBarang.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                        Cache.SetCache(cacheKey, dt, 300);
                    }
                }
                catch (Exception ex)
                {
                    DarkModeMessageBox.Show("Gagal memuat data barang: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void dgvBarang_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvBarang.Rows[e.RowIndex];

                inputIdBarang.Texts = row.Cells["ID Barang"].Value.ToString();
                inputNamaBarang.Texts = row.Cells["Nama Barang"].Value.ToString();
                inputJumlah.Texts = row.Cells["Jumlah"].Value.ToString();
                inputSatuan.Texts = row.Cells["Satuan"].Value.ToString();

                inputIdBarang.ReadOnly = true;

                _origNamaBarang = inputNamaBarang.Texts;
                _origSatuan = inputSatuan.Texts;
                _origJumlah = int.TryParse(inputJumlah.Texts, out int jumlahParsed) ? jumlahParsed : 0;
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!Session.CekSession()) return;

            if (inputIdBarang.Texts == "" || inputNamaBarang.Texts == "" || inputJumlah.Texts == "")
            {
                DarkModeMessageBox.Show("Isi semua data yang wajib yaa~", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(inputJumlah.Texts.Trim(), out int jumlah))
            {
                DarkModeMessageBox.Show("Jumlah harus angka, bukan huruf!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (inputNamaBarang.Texts.Trim() == _origNamaBarang &&
                inputSatuan.Texts.Trim() == _origSatuan &&
                jumlah == _origJumlah)
            {
                DarkModeMessageBox.Show("Gak ada yang berubah, jangan boongin aku dong~ hmph!", "Info",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("sp_UpdateBarangKonsumsi", conn, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@id_barang", inputIdBarang.Texts.Trim());
                                cmd.Parameters.AddWithValue("@nama_barang", inputNamaBarang.Texts.Trim());
                                cmd.Parameters.AddWithValue("@jumlah", jumlah);
                                cmd.Parameters.AddWithValue("@satuan", inputSatuan.Texts.Trim());
                                cmd.Parameters.AddWithValue("@id_akun", Session.LoggedInUserId);

                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    transaction.Commit();
                                    DarkModeMessageBox.Show("Barang berhasil diupdate, nice~", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    Cache.RemoveCache($"Cache:BarangKonsumsi_{Session.LoggedInUserId}");
                                    LoadDataBarang();
                                }
                                else
                                {
                                    transaction.Rollback();
                                    DarkModeMessageBox.Show("Update gagal, cek ID-nya bener nggak!", "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DarkModeMessageBox.Show("Error update data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SearchDataBarangKonsumsi(string keyword)
        {
            if (!Session.CekSession()) return;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_SearchBarangKonsumsi", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id_akun", Session.LoggedInUserId);
                        if (string.IsNullOrEmpty(keyword))
                            cmd.Parameters.AddWithValue("@keyword", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@keyword", keyword);

                        DataTable dt = new DataTable();
                        new SqlDataAdapter(cmd).Fill(dt);

                        dgvBarang.DataSource = dt;
                        dgvBarang.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    }
                }
                catch (Exception ex)
                {
                    DarkModeMessageBox.Show("Gagal search barang konsumsi: " + ex.Message,
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void InputSearchBarang_TextChanged(object sender, EventArgs e)
        {
            Cache.ClearCache($"Cache:BarangKonsumsi_{Session.LoggedInUserId}");

            string kw = inputSearchBarang.Texts.Trim();
            SearchDataBarangKonsumsi(string.IsNullOrEmpty(kw) ? null : kw);
        }

        private void btnKembali_Click(object sender, EventArgs e)
        {
            this.Hide();
            FormKDB form = new FormKDB();
            form.Show();
            form.EnableAutoResize();
            form.WindowState = FormWindowState.Normal;
            if (this.WindowState == FormWindowState.Maximized)
                form.WindowState = FormWindowState.Maximized;
        }

        private void btnShowCacheBarang_Click(object sender, EventArgs e)
        {
            string info = Cache.GetCachePreview($"Cache:BarangKonsumsi_{Session.LoggedInUserId}");
            DarkModeMessageBox.Show(info, "Informasi Cache Barang Konsumsi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnRefreshDataBarang_Click(object sender, EventArgs e)
        {
            Cache.ClearCache($"Cache:BarangKonsumsi_{Session.LoggedInUserId}");
            LoadDataBarang();
            FormToast toast = new FormToast("Refresh Berhasil", 1500);
            toast.Show();
        }
    }
}