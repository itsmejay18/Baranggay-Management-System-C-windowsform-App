using System;

using System.Collections.Generic;

using System.ComponentModel;

using System.Data;

using System.Drawing;

using System.IO;

using System.Windows.Forms;

using MySql.Data.MySqlClient;

using baranggaysystem1.Database;



namespace baranggaysystem1;



internal partial class ResidentForm : Form
{
	private readonly int? _residentId;

	private byte[]? _photoBytes;
	private readonly ResidentFormController _controller;
	private Label? lblBarangay;
	private Label? lblPurok;
	private Label? lblHousehold;
	private ComboBox? cmbBarangay;
	private ComboBox? cmbPurok;
	private ComboBox? cmbHousehold;
	private bool _locationReady;
	private bool _suppressLocationEvents;
	private List<LookupItem>? _barangayCache;
	private readonly Dictionary<int, List<LookupItem>> _purokCache = new Dictionary<int, List<LookupItem>>();
	private readonly Dictionary<string, List<LookupItem>> _householdCache = new Dictionary<string, List<LookupItem>>();


	public ResidentDto Resident => new ResidentDto

	{

		Id = _residentId,

		FirstName = txtFirstName.Text.Trim(),

		MiddleName = txtMiddleName.Text.Trim(),

		LastName = txtLastName.Text.Trim(),

		Gender = (cmbGender.SelectedItem?.ToString() ?? cmbGender.Text.Trim()),

		DateOfBirth = dtpBirthDate.Value.Date,

		CivilStatus = (cmbCivilStatus.SelectedItem?.ToString() ?? cmbCivilStatus.Text.Trim()),

		ContactNo = txtContact.Text.Trim(),

		Status = (cmbStatus.SelectedItem?.ToString() ?? cmbStatus.Text.Trim()),

		PhotoBytes = _photoBytes,

		BarangayId = GetSelectedLookupId(cmbBarangay),

		PurokId = GetSelectedLookupId(cmbPurok),

		HouseholdId = GetSelectedLookupId(cmbHousehold)

	};



	public ResidentForm(string title, ResidentDto? existing = null)
	{
		_residentId = existing?.Id;
		InitializeComponent();
		InitializeLocationFields();
		_controller = new ResidentFormController(this);
		Text = title;
		ApplyTheme();
		LoadLocationLookups();
		if (existing != null)

		{

			Populate(existing);

		}

		UpdatePhotoPreview();

	}



	private void ApplyTheme()

	{

		BackColor = UiTheme.Slate50;

		Font = UiTheme.BodyFont;

		base.FormBorderStyle = FormBorderStyle.FixedDialog;

		base.StartPosition = FormStartPosition.CenterParent;

		base.MaximizeBox = false;

		base.MinimizeBox = false;

		UiTheme.StyleTextBoxes(txtFirstName, txtMiddleName, txtLastName, txtContact);

		UiTheme.StyleComboBoxes(cmbGender, cmbCivilStatus, cmbStatus);
		if (cmbBarangay != null)
		{
			UiTheme.StyleComboBox(cmbBarangay);
		}
		if (cmbPurok != null)
		{
			UiTheme.StyleComboBox(cmbPurok);
		}
		if (cmbHousehold != null)
		{
			UiTheme.StyleComboBox(cmbHousehold);
		}



		UiTheme.StyleSecondaryButtons(btnPhotoUpload, btnCancel);

		UiTheme.StyleDangerButton(btnPhotoRemove);

		UiTheme.StylePrimaryButton(btnSave);

		lblHeader.Font = UiTheme.HeadingFont;

		lblHeader.ForeColor = UiTheme.Slate900;

		UiTheme.ApplyLabelFont(UiTheme.LabelFont, lblSubHeader, lblFirstName, lblMiddleName, lblLastName, lblGender, lblBirthDate, lblCivilStatus, lblContact, lblStatus, lblPhotoCaption);
		var locationLabels = new List<Label>();
		if (lblBarangay != null)
		{
			locationLabels.Add(lblBarangay);
		}
		if (lblPurok != null)
		{
			locationLabels.Add(lblPurok);
		}
		if (lblHousehold != null)
		{
			locationLabels.Add(lblHousehold);
		}
		if (locationLabels.Count > 0)
		{
			UiTheme.ApplyLabelFont(UiTheme.LabelFont, locationLabels.ToArray());
		}

		lblSubHeader.ForeColor = UiTheme.Slate500;

		lblFirstName.ForeColor = UiTheme.Slate700;

		lblMiddleName.ForeColor = UiTheme.Slate700;

		lblLastName.ForeColor = UiTheme.Slate700;

		lblGender.ForeColor = UiTheme.Slate700;

		lblBirthDate.ForeColor = UiTheme.Slate700;

		lblCivilStatus.ForeColor = UiTheme.Slate700;

		lblContact.ForeColor = UiTheme.Slate700;

		lblStatus.ForeColor = UiTheme.Slate700;

		lblPhotoCaption.ForeColor = UiTheme.Slate500;
		if (lblBarangay != null)
		{
			lblBarangay.ForeColor = UiTheme.Slate700;
		}
		if (lblPurok != null)
		{
			lblPurok.ForeColor = UiTheme.Slate700;
		}
		if (lblHousehold != null)
		{
			lblHousehold.ForeColor = UiTheme.Slate700;
		}
		UiTheme.StandardizeButtonLayout(this);

	}


	private void InitializeLocationFields()
	{
		if (_locationReady)
		{
			return;
		}

		lblBarangay = new Label { Text = "Barangay", AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
		lblPurok = new Label { Text = "Purok/Sitio", AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
		lblHousehold = new Label { Text = "Household", AutoSize = true, Margin = new Padding(3, 8, 3, 3) };

		cmbBarangay = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 5, 3, 5) };
		cmbPurok = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 5, 3, 5) };
		cmbHousehold = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 5, 3, 5) };

		if (txtFirstName != null)
		{
			cmbBarangay.Size = txtFirstName.Size;
			cmbPurok.Size = txtFirstName.Size;
			cmbHousehold.Size = txtFirstName.Size;
		}

		if (fieldsTable != null)
		{
			int row = fieldsTable.RowCount;
			fieldsTable.RowCount += 3;
			fieldsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			fieldsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			fieldsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			fieldsTable.Controls.Add(lblBarangay, 0, row);
			fieldsTable.Controls.Add(cmbBarangay, 1, row);
			fieldsTable.Controls.Add(lblPurok, 0, row + 1);
			fieldsTable.Controls.Add(cmbPurok, 1, row + 1);
			fieldsTable.Controls.Add(lblHousehold, 0, row + 2);
			fieldsTable.Controls.Add(cmbHousehold, 1, row + 2);
		}

		if (cmbBarangay != null)
		{
			cmbBarangay.SelectedIndexChanged -= BarangaySelectionChanged;
			cmbBarangay.SelectedIndexChanged += BarangaySelectionChanged;
		}
		if (cmbPurok != null)
		{
			cmbPurok.SelectedIndexChanged -= PurokSelectionChanged;
			cmbPurok.SelectedIndexChanged += PurokSelectionChanged;
		}

		_locationReady = true;
	}


	private void LoadLocationLookups()
	{
		if (!_locationReady || cmbBarangay == null || cmbPurok == null || cmbHousehold == null)
		{
			return;
		}

		try
		{
			var barangays = _barangayCache ??= LoadLookupItems("SELECT barangay_id, name FROM barangay ORDER BY name");
			_suppressLocationEvents = true;
			BindCombo(cmbBarangay, barangays, includeNone: false);
			SelectComboById(cmbBarangay, SchemaDefaults.DefaultBarangayId);
			int barangayId = GetSelectedLookupId(cmbBarangay) ?? SchemaDefaults.DefaultBarangayId;
			ReloadPurokList(barangayId, SchemaDefaults.DefaultPurokId);
			int? purokId = GetSelectedLookupId(cmbPurok);
			ReloadHouseholdList(barangayId, purokId, null);
		}
		catch (Exception ex)
		{
			ControllerDialogs.Error(ex, "Unable to load location data.", "Location Error");
		}
		finally
		{
			_suppressLocationEvents = false;
		}
	}


	private void BarangaySelectionChanged(object? sender, EventArgs e)
	{
		if (_suppressLocationEvents || cmbBarangay == null)
		{
			return;
		}

		try
		{
			int barangayId = GetSelectedLookupId(cmbBarangay) ?? SchemaDefaults.DefaultBarangayId;
			ReloadPurokList(barangayId, null);
			int? purokId = GetSelectedLookupId(cmbPurok);
			ReloadHouseholdList(barangayId, purokId, null);
		}
		catch (Exception ex)
		{
			ControllerDialogs.Error(ex, "Unable to load purok list.", "Location Error");
		}
	}


	private void PurokSelectionChanged(object? sender, EventArgs e)
	{
		if (_suppressLocationEvents || cmbBarangay == null || cmbPurok == null)
		{
			return;
		}

		try
		{
			int barangayId = GetSelectedLookupId(cmbBarangay) ?? SchemaDefaults.DefaultBarangayId;
			int? purokId = GetSelectedLookupId(cmbPurok);
			ReloadHouseholdList(barangayId, purokId, null);
		}
		catch (Exception ex)
		{
			ControllerDialogs.Error(ex, "Unable to load household list.", "Location Error");
		}
	}


	private static List<LookupItem> LoadLookupItems(string sql, params MySqlParameter[] parameters)
	{
		var items = new List<LookupItem>();
		DataTable table = DbHelper.LoadTable(sql, cmd =>
		{
			if (parameters != null && parameters.Length > 0)
			{
				cmd.Parameters.AddRange(parameters);
			}
		});

		foreach (DataRow row in table.Rows)
		{
			if (row[0] == DBNull.Value)
			{
				continue;
			}

			int id = Convert.ToInt32(row[0]);
			string name = row[1] == DBNull.Value ? $"#{id}" : Convert.ToString(row[1]) ?? $"#{id}";
			items.Add(new LookupItem(id, name));
		}
		return items;
	}


	private static void BindCombo(ComboBox comboBox, List<LookupItem> items, bool includeNone)
	{
		var data = includeNone ? new List<LookupItem> { new LookupItem(0, "(None)") } : new List<LookupItem>();
		data.AddRange(items);

		comboBox.DataSource = null;
		comboBox.DisplayMember = nameof(LookupItem.Name);
		comboBox.ValueMember = nameof(LookupItem.Id);
		comboBox.DataSource = data;
	}


	private void ReloadPurokList(int barangayId, int? selectedId)
	{
		if (cmbPurok == null)
		{
			return;
		}

		if (!_purokCache.TryGetValue(barangayId, out List<LookupItem>? puroks))
		{
			puroks = LoadLookupItems(
				"SELECT purok_id, name FROM purok_sitio WHERE barangay_id = @barangayId ORDER BY name",
				new MySqlParameter("@barangayId", barangayId));
			_purokCache[barangayId] = puroks;
		}

		BindCombo(cmbPurok, puroks, includeNone: false);
		SelectComboById(cmbPurok, selectedId ?? SchemaDefaults.DefaultPurokId);
	}


	private void ReloadHouseholdList(int barangayId, int? purokId, int? selectedId)
	{
		if (cmbHousehold == null)
		{
			return;
		}

		string sql = @"SELECT household_id,
                              COALESCE(NULLIF(TRIM(CONCAT_WS(' ', house_no, street, subdivision)), ''), CONCAT('Household #', household_id)) AS label
                       FROM household
                       WHERE barangay_id = @barangayId
                         AND (@purokId IS NULL OR purok_id = @purokId)
                       ORDER BY household_id";
		string cacheKey = $"{barangayId}:{(purokId.HasValue ? purokId.Value.ToString() : "null")}";
		if (!_householdCache.TryGetValue(cacheKey, out List<LookupItem>? households))
		{
			households = LoadLookupItems(sql,
				new MySqlParameter("@barangayId", barangayId),
				new MySqlParameter("@purokId", (object?)purokId ?? DBNull.Value));
			_householdCache[cacheKey] = households;
		}

		BindCombo(cmbHousehold, households, includeNone: true);
		SelectComboById(cmbHousehold, selectedId);
	}



	private void PhotoUpload_Click(object? sender, EventArgs e)
	{
		_controller.HandlePhotoUpload();
	}


	private void PhotoRemove_Click(object? sender, EventArgs e)
	{
		_controller.HandlePhotoRemove();
	}


	private void UpdatePhotoPreview()

	{

		if (_photoBytes == null || _photoBytes.Length == 0)

		{

			picPhoto.Image = null;

			lblPhotoCaption.Text = "No photo";

			btnPhotoRemove.Enabled = false;

			return;

		}

		try

		{

			using MemoryStream stream = new MemoryStream(_photoBytes);

			picPhoto.Image = Image.FromStream(stream);

			lblPhotoCaption.Text = "Photo selected";

			btnPhotoRemove.Enabled = true;

		}

		catch

		{

			picPhoto.Image = null;

			lblPhotoCaption.Text = "Invalid photo";

			btnPhotoRemove.Enabled = false;

		}

	}



	private static void SelectComboValue(ComboBox comboBox, string value)

	{

		if (comboBox.Items.Count == 0)

		{

			return;

		}

		for (int i = 0; i < comboBox.Items.Count; i++)

		{

			if (string.Equals(comboBox.Items[i]?.ToString(), value, StringComparison.OrdinalIgnoreCase))

			{

				comboBox.SelectedIndex = i;

				return;

			}

		}

		comboBox.SelectedIndex = 0;

	}



	private void Populate(ResidentDto resident)

	{

		txtFirstName.Text = resident.FirstName;

		txtMiddleName.Text = resident.MiddleName;

		txtLastName.Text = resident.LastName;

		SelectComboValue(cmbGender, resident.Gender);

		dtpBirthDate.Value = resident.DateOfBirth;

		SelectComboValue(cmbCivilStatus, resident.CivilStatus);

		txtContact.Text = resident.ContactNo;

		SelectComboValue(cmbStatus, resident.Status);

		if (cmbBarangay != null && cmbPurok != null && cmbHousehold != null)
		{
			try
			{
				_suppressLocationEvents = true;
				SelectComboById(cmbBarangay, resident.BarangayId ?? SchemaDefaults.DefaultBarangayId);
				int barangayId = GetSelectedLookupId(cmbBarangay) ?? SchemaDefaults.DefaultBarangayId;
				ReloadPurokList(barangayId, resident.PurokId ?? SchemaDefaults.DefaultPurokId);
				int? purokId = GetSelectedLookupId(cmbPurok);
				ReloadHouseholdList(barangayId, purokId, resident.HouseholdId);
			}
			finally
			{
				_suppressLocationEvents = false;
			}
		}

		_photoBytes = resident.PhotoBytes;

		UpdatePhotoPreview();

	}



	private void ValidateAndClose(object? sender, EventArgs e)
	{
		_controller.HandleSave();
	}


	



	private static int? GetSelectedLookupId(ComboBox? comboBox)
	{
		if (comboBox == null)
		{
			return null;
		}

		if (comboBox.SelectedValue is int idValue)
		{
			return idValue == 0 ? (int?)null : idValue;
		}

		if (comboBox.SelectedItem is LookupItem item)
		{
			return item.Id == 0 ? (int?)null : item.Id;
		}

		return null;
	}


	private static void SelectComboById(ComboBox comboBox, int? id)
	{
		if (comboBox.Items.Count == 0)
		{
			return;
		}

		if (id.HasValue)
		{
			comboBox.SelectedValue = id.Value;
			if (comboBox.SelectedIndex >= 0)
			{
				return;
			}
		}

		comboBox.SelectedIndex = 0;
	}

}



