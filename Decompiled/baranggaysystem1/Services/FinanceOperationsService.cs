using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using baranggaysystem1.Database;
using baranggaysystem1.Models;
using baranggaysystem1.helper;

namespace baranggaysystem1.Services;

internal sealed class FinanceOperationsService
{
	private static readonly string[] AllowedProcurementTypes = new string[3] { "PROCUREMENT", "PURCHASE ORDER", "EMERGENCY PURCHASE" };

	private static readonly string[] AllowedProcurementStatuses = new string[6] { "DRAFT", "FOR APPROVAL", "APPROVED", "ORDERED", "RECEIVED", "CANCELLED" };

	public async Task<DataTable> GetExpenseLedgerAsync()
	{
		return await DatabaseManagerAsync.LoadTableAsync("\n                SELECT e.expense_id,\n                       e.expense_date,\n                       DATE_FORMAT(e.expense_date, '%Y-%m-%d') AS expense_date_display,\n                       e.expense_category,\n                       e.expense_title,\n                       COALESCE(e.payee_name, '') AS payee_name,\n                       IFNULL(e.amount, 0.00) AS amount,\n                       COALESCE(e.payment_method, 'Cash') AS payment_method,\n                       COALESCE(e.status, 'POSTED') AS status,\n                       COALESCE(e.reference_no, '') AS reference_no,\n                       COALESCE(e.notes, '') AS notes,\n                       DATE_FORMAT(e.created_at, '%Y-%m-%d %h:%i %p') AS created_at_display\n                FROM expense_entry e\n                WHERE e.barangay_id = @barangayId\n                ORDER BY e.expense_date DESC, e.expense_id DESC\n                LIMIT 300", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<DataTable> GetInventoryLedgerAsync()
	{
		return await DatabaseManagerAsync.LoadTableAsync("\n                SELECT i.item_id,\n                       i.item_name,\n                       i.category,\n                       COALESCE(i.unit, 'pcs') AS unit,\n                       IFNULL(i.quantity_on_hand, 0.00) AS quantity_on_hand,\n                       IFNULL(i.reorder_level, 0.00) AS reorder_level,\n                       IFNULL(i.unit_cost, 0.00) AS unit_cost,\n                       IFNULL(i.quantity_on_hand, 0.00) * IFNULL(i.unit_cost, 0.00) AS stock_value,\n                       COALESCE(i.location, '') AS location,\n                       COALESCE(i.item_status, 'ACTIVE') AS item_status,\n                       i.last_restocked_at,\n                       DATE_FORMAT(i.last_restocked_at, '%Y-%m-%d') AS last_restocked_display,\n                       COALESCE(i.notes, '') AS notes,\n                       CASE\n                           WHEN UPPER(COALESCE(i.item_status, 'ACTIVE')) = 'ARCHIVED' THEN 'ARCHIVED'\n                           WHEN IFNULL(i.quantity_on_hand, 0.00) <= 0 THEN 'OUT OF STOCK'\n                           WHEN IFNULL(i.quantity_on_hand, 0.00) <= IFNULL(i.reorder_level, 0.00) THEN 'LOW STOCK'\n                           ELSE 'IN STOCK'\n                       END AS stock_state\n                FROM inventory_item i\n                WHERE i.barangay_id = @barangayId\n                ORDER BY CASE\n                    WHEN UPPER(COALESCE(i.item_status, 'ACTIVE')) = 'ARCHIVED' THEN 1\n                    ELSE 0\n                END,\n                i.item_name ASC,\n                i.item_id DESC\n                LIMIT 300", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<DataTable> GetAssetLedgerAsync()
	{
		return await DatabaseManagerAsync.LoadTableAsync("\n                SELECT a.asset_id,\n                       a.asset_name,\n                       a.asset_category,\n                       COALESCE(a.asset_tag, '') AS asset_tag,\n                       a.acquisition_date,\n                       DATE_FORMAT(a.acquisition_date, '%Y-%m-%d') AS acquisition_date_display,\n                       IFNULL(a.acquisition_cost, 0.00) AS acquisition_cost,\n                       COALESCE(a.assigned_location, '') AS assigned_location,\n                       COALESCE(a.custodian_name, '') AS custodian_name,\n                       COALESCE(a.condition_status, 'GOOD') AS condition_status,\n                       COALESCE(a.lifecycle_status, 'ACTIVE') AS lifecycle_status,\n                       COALESCE(a.notes, '') AS notes,\n                       DATE_FORMAT(a.created_at, '%Y-%m-%d %h:%i %p') AS created_at_display\n                FROM asset_record a\n                WHERE a.barangay_id = @barangayId\n                ORDER BY CASE\n                    WHEN UPPER(COALESCE(a.lifecycle_status, 'ACTIVE')) = 'ACTIVE' THEN 0\n                    ELSE 1\n                END,\n                a.asset_name ASC,\n                a.asset_id DESC\n                LIMIT 300", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<DataTable> GetProcurementLedgerAsync()
	{
		return await DatabaseManagerAsync.LoadTableAsync("\n                SELECT p.procurement_id,\n                       COALESCE(p.request_type, 'PROCUREMENT') AS request_type,\n                       p.request_date,\n                       DATE_FORMAT(p.request_date, '%Y-%m-%d') AS request_date_display,\n                       p.needed_by_date,\n                       DATE_FORMAT(p.needed_by_date, '%Y-%m-%d') AS needed_by_display,\n                       p.request_title,\n                       p.procurement_category,\n                       COALESCE(p.vendor_name, '') AS vendor_name,\n                       COALESCE(p.requested_by_name, '') AS requested_by_name,\n                       IFNULL(p.total_amount, 0.00) AS total_amount,\n                       COALESCE(p.workflow_status, 'DRAFT') AS workflow_status,\n                       COALESCE(p.purchase_order_no, '') AS purchase_order_no,\n                       COALESCE(p.approved_by_name, '') AS approved_by_name,\n                       p.approved_at,\n                       DATE_FORMAT(p.approved_at, '%Y-%m-%d %h:%i %p') AS approved_at_display,\n                       COALESCE(p.item_summary, '') AS item_summary,\n                       COALESCE(p.approval_notes, '') AS approval_notes,\n                       COALESCE(p.notes, '') AS notes,\n                       DATE_FORMAT(p.created_at, '%Y-%m-%d %h:%i %p') AS created_at_display\n                FROM procurement_request p\n                WHERE p.barangay_id = @barangayId\n                ORDER BY CASE UPPER(COALESCE(p.workflow_status, 'DRAFT'))\n                    WHEN 'FOR APPROVAL' THEN 0\n                    WHEN 'APPROVED' THEN 1\n                    WHEN 'ORDERED' THEN 2\n                    WHEN 'DRAFT' THEN 3\n                    WHEN 'RECEIVED' THEN 4\n                    ELSE 5\n                END,\n                p.request_date DESC,\n                p.procurement_id DESC\n                LIMIT 300", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task SaveExpenseAsync(ExpenseEntryRecord record)
	{
		ExpenseEntryRecord sanitized = Sanitize(record);
		if (sanitized.ExpenseId > 0)
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                    UPDATE expense_entry\n                    SET expense_date = @expenseDate,\n                        expense_category = @expenseCategory,\n                        expense_title = @expenseTitle,\n                        payee_name = @payeeName,\n                        amount = @amount,\n                        payment_method = @paymentMethod,\n                        status = @status,\n                        reference_no = @referenceNo,\n                        notes = @notes,\n                        updated_by_user_id = @userId,\n                        updated_at = NOW()\n                    WHERE expense_id = @expenseId\n                      AND barangay_id = @barangayId", delegate(MySqlCommand cmd)
			{
				ConfigureExpenseParameters(cmd, sanitized);
				cmd.Parameters.AddWithValue("@expenseId", (object)sanitized.ExpenseId);
				cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
				cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                INSERT INTO expense_entry\n                    (barangay_id, expense_date, expense_category, expense_title, payee_name, amount, payment_method, status, reference_no, notes, created_by_user_id, updated_by_user_id, created_at, updated_at)\n                VALUES\n                    (@barangayId, @expenseDate, @expenseCategory, @expenseTitle, @payeeName, @amount, @paymentMethod, @status, @referenceNo, @notes, @userId, @userId, NOW(), NOW())", delegate(MySqlCommand cmd)
			{
				ConfigureExpenseParameters(cmd, sanitized);
				cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
				cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async Task SaveInventoryItemAsync(InventoryItemRecord record)
	{
		InventoryItemRecord sanitized = Sanitize(record);
		if (sanitized.ItemId > 0)
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                    UPDATE inventory_item\n                    SET item_name = @itemName,\n                        category = @category,\n                        unit = @unit,\n                        quantity_on_hand = @quantityOnHand,\n                        reorder_level = @reorderLevel,\n                        unit_cost = @unitCost,\n                        location = @location,\n                        item_status = @itemStatus,\n                        last_restocked_at = @lastRestockedAt,\n                        notes = @notes,\n                        updated_by_user_id = @userId,\n                        updated_at = NOW()\n                    WHERE item_id = @itemId\n                      AND barangay_id = @barangayId", delegate(MySqlCommand cmd)
			{
				ConfigureInventoryParameters(cmd, sanitized);
				cmd.Parameters.AddWithValue("@itemId", (object)sanitized.ItemId);
				cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
				cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                INSERT INTO inventory_item\n                    (barangay_id, item_name, category, unit, quantity_on_hand, reorder_level, unit_cost, location, item_status, last_restocked_at, notes, created_by_user_id, updated_by_user_id, created_at, updated_at)\n                VALUES\n                    (@barangayId, @itemName, @category, @unit, @quantityOnHand, @reorderLevel, @unitCost, @location, @itemStatus, @lastRestockedAt, @notes, @userId, @userId, NOW(), NOW())", delegate(MySqlCommand cmd)
			{
				ConfigureInventoryParameters(cmd, sanitized);
				cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
				cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async Task SaveAssetAsync(AssetRecord record)
	{
		AssetRecord sanitized = Sanitize(record);
		if (sanitized.AssetId > 0)
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                    UPDATE asset_record\n                    SET asset_name = @assetName,\n                        asset_category = @assetCategory,\n                        asset_tag = @assetTag,\n                        acquisition_date = @acquisitionDate,\n                        acquisition_cost = @acquisitionCost,\n                        assigned_location = @assignedLocation,\n                        custodian_name = @custodianName,\n                        condition_status = @conditionStatus,\n                        lifecycle_status = @lifecycleStatus,\n                        notes = @notes,\n                        updated_by_user_id = @userId,\n                        updated_at = NOW()\n                    WHERE asset_id = @assetId\n                      AND barangay_id = @barangayId", delegate(MySqlCommand cmd)
			{
				ConfigureAssetParameters(cmd, sanitized);
				cmd.Parameters.AddWithValue("@assetId", (object)sanitized.AssetId);
				cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
				cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                INSERT INTO asset_record\n                    (barangay_id, asset_name, asset_category, asset_tag, acquisition_date, acquisition_cost, assigned_location, custodian_name, condition_status, lifecycle_status, notes, created_by_user_id, updated_by_user_id, created_at, updated_at)\n                VALUES\n                    (@barangayId, @assetName, @assetCategory, @assetTag, @acquisitionDate, @acquisitionCost, @assignedLocation, @custodianName, @conditionStatus, @lifecycleStatus, @notes, @userId, @userId, NOW(), NOW())", delegate(MySqlCommand cmd)
			{
				ConfigureAssetParameters(cmd, sanitized);
				cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
				cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async Task SaveProcurementRequestAsync(ProcurementRequestRecord record)
	{
		ProcurementRequestRecord sanitized = Sanitize(record);
		sanitized.RequestedByName = (string.IsNullOrWhiteSpace(sanitized.RequestedByName) ? GetUserDisplayName() : sanitized.RequestedByName);
		if (sanitized.ProcurementId > 0)
		{
			ProcurementRequestRecord procurementRequestRecord = await GetProcurementRequestAsync(sanitized.ProcurementId).ConfigureAwait(continueOnCapturedContext: false);
			if (procurementRequestRecord == null)
			{
				throw new InvalidOperationException("The selected procurement request could not be found.");
			}
			string approvedByName = procurementRequestRecord.ApprovedByName;
			DateTime? approvedAt = procurementRequestRecord.ApprovedAt;
			if (ShouldStampApproval(sanitized.WorkflowStatus, procurementRequestRecord))
			{
				approvedByName = GetUserDisplayName();
				approvedAt = DateTime.Now;
			}
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                    UPDATE procurement_request\n                    SET request_type = @requestType,\n                        request_date = @requestDate,\n                        needed_by_date = @neededByDate,\n                        request_title = @requestTitle,\n                        procurement_category = @procurementCategory,\n                        vendor_name = @vendorName,\n                        requested_by_name = @requestedByName,\n                        total_amount = @totalAmount,\n                        workflow_status = @workflowStatus,\n                        purchase_order_no = @purchaseOrderNo,\n                        approved_by_name = @approvedByName,\n                        approved_at = @approvedAt,\n                        item_summary = @itemSummary,\n                        approval_notes = @approvalNotes,\n                        notes = @notes,\n                        updated_by_user_id = @userId,\n                        updated_at = NOW()\n                    WHERE procurement_id = @procurementId\n                      AND barangay_id = @barangayId", delegate(MySqlCommand cmd)
			{
				ConfigureProcurementParameters(cmd, sanitized, approvedByName, approvedAt);
				cmd.Parameters.AddWithValue("@procurementId", (object)sanitized.ProcurementId);
				cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
				cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			string newApprovedByName = (ShouldCarryApprovalTrail(sanitized.WorkflowStatus) ? GetUserDisplayName() : string.Empty);
			DateTime? newApprovedAt = (ShouldCarryApprovalTrail(sanitized.WorkflowStatus) ? new DateTime?(DateTime.Now) : ((DateTime?)null));
			await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                INSERT INTO procurement_request\n                    (barangay_id, request_type, request_date, needed_by_date, request_title, procurement_category, vendor_name, requested_by_name, total_amount, workflow_status, purchase_order_no, approved_by_name, approved_at, item_summary, approval_notes, notes, created_by_user_id, updated_by_user_id, created_at, updated_at)\n                VALUES\n                    (@barangayId, @requestType, @requestDate, @neededByDate, @requestTitle, @procurementCategory, @vendorName, @requestedByName, @totalAmount, @workflowStatus, @purchaseOrderNo, @approvedByName, @approvedAt, @itemSummary, @approvalNotes, @notes, @userId, @userId, NOW(), NOW())", delegate(MySqlCommand cmd)
			{
				ConfigureProcurementParameters(cmd, sanitized, newApprovedByName, newApprovedAt);
				cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
				cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public async Task AdvanceProcurementWorkflowAsync(int procurementId)
	{
		ProcurementRequestRecord procurementRequestRecord = await GetProcurementRequestAsync(procurementId).ConfigureAwait(continueOnCapturedContext: false);
		if (procurementRequestRecord == null)
		{
			throw new InvalidOperationException("The selected procurement request could not be found.");
		}
		string nextStatus = NormalizeDefault(procurementRequestRecord.WorkflowStatus, "DRAFT") switch
		{
			"DRAFT" => "FOR APPROVAL", 
			"FOR APPROVAL" => "APPROVED", 
			"APPROVED" => "ORDERED", 
			"ORDERED" => "RECEIVED", 
			"RECEIVED" => throw new InvalidOperationException("This procurement request has already been completed."), 
			"CANCELLED" => throw new InvalidOperationException("Cancelled procurement requests cannot be advanced."), 
			_ => throw new InvalidOperationException("The selected procurement request is in an unsupported workflow state."), 
		};
		string approvedByName = procurementRequestRecord.ApprovedByName;
		DateTime? approvedAt = procurementRequestRecord.ApprovedAt;
		if (nextStatus == "APPROVED" && string.IsNullOrWhiteSpace(approvedByName))
		{
			approvedByName = GetUserDisplayName();
			approvedAt = DateTime.Now;
		}
		await DatabaseManagerAsync.ExecuteNonQueryAsync("\n                UPDATE procurement_request\n                SET workflow_status = @workflowStatus,\n                    approved_by_name = @approvedByName,\n                    approved_at = @approvedAt,\n                    updated_by_user_id = @userId,\n                    updated_at = NOW()\n                WHERE procurement_id = @procurementId\n                  AND barangay_id = @barangayId", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@workflowStatus", (object)nextStatus);
			cmd.Parameters.AddWithValue("@approvedByName", NormalizeNullable(approvedByName));
			cmd.Parameters.AddWithValue("@approvedAt", NormalizeNullableDateTime(approvedAt));
			cmd.Parameters.AddWithValue("@userId", GetUserIdOrNull());
			cmd.Parameters.AddWithValue("@procurementId", (object)procurementId);
			cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static ExpenseEntryRecord Sanitize(ExpenseEntryRecord record)
	{
		if (record == null)
		{
			throw new InvalidOperationException("Expense record is required.");
		}
		string expenseCategory = NormalizeRequired(record.ExpenseCategory, "Expense category is required.");
		string expenseTitle = NormalizeRequired(record.ExpenseTitle, "Expense title is required.");
		decimal num = decimal.Round(record.Amount, 2, MidpointRounding.AwayFromZero);
		if (num <= 0m)
		{
			throw new InvalidOperationException("Expense amount must be greater than zero.");
		}
		return new ExpenseEntryRecord
		{
			ExpenseId = record.ExpenseId,
			ExpenseDate = ((record.ExpenseDate == default(DateTime)) ? DateTime.Today : record.ExpenseDate.Date),
			ExpenseCategory = expenseCategory,
			ExpenseTitle = expenseTitle,
			PayeeName = NormalizeOptional(record.PayeeName),
			Amount = num,
			PaymentMethod = (string.IsNullOrWhiteSpace(record.PaymentMethod) ? "Cash" : record.PaymentMethod.Trim()),
			Status = NormalizeDefault(record.Status, "POSTED"),
			ReferenceNo = NormalizeOptional(record.ReferenceNo),
			Notes = NormalizeOptional(record.Notes)
		};
	}

	private static InventoryItemRecord Sanitize(InventoryItemRecord record)
	{
		if (record == null)
		{
			throw new InvalidOperationException("Inventory item is required.");
		}
		decimal num = decimal.Round(record.QuantityOnHand, 2, MidpointRounding.AwayFromZero);
		decimal num2 = decimal.Round(record.ReorderLevel, 2, MidpointRounding.AwayFromZero);
		decimal num3 = decimal.Round(record.UnitCost, 2, MidpointRounding.AwayFromZero);
		if (num < 0m)
		{
			throw new InvalidOperationException("Quantity on hand cannot be negative.");
		}
		if (num2 < 0m)
		{
			throw new InvalidOperationException("Reorder level cannot be negative.");
		}
		if (num3 < 0m)
		{
			throw new InvalidOperationException("Unit cost cannot be negative.");
		}
		return new InventoryItemRecord
		{
			ItemId = record.ItemId,
			ItemName = NormalizeRequired(record.ItemName, "Item name is required."),
			Category = NormalizeRequired(record.Category, "Inventory category is required."),
			Unit = NormalizeDefault(record.Unit, "pcs"),
			QuantityOnHand = num,
			ReorderLevel = num2,
			UnitCost = num3,
			Location = NormalizeOptional(record.Location),
			ItemStatus = NormalizeDefault(record.ItemStatus, "ACTIVE"),
			LastRestockedAt = record.LastRestockedAt?.Date,
			Notes = NormalizeOptional(record.Notes)
		};
	}

	private static AssetRecord Sanitize(AssetRecord record)
	{
		if (record == null)
		{
			throw new InvalidOperationException("Asset record is required.");
		}
		decimal num = decimal.Round(record.AcquisitionCost, 2, MidpointRounding.AwayFromZero);
		if (num < 0m)
		{
			throw new InvalidOperationException("Acquisition cost cannot be negative.");
		}
		return new AssetRecord
		{
			AssetId = record.AssetId,
			AssetName = NormalizeRequired(record.AssetName, "Asset name is required."),
			AssetCategory = NormalizeRequired(record.AssetCategory, "Asset category is required."),
			AssetTag = NormalizeOptional(record.AssetTag),
			AcquisitionDate = record.AcquisitionDate?.Date,
			AcquisitionCost = num,
			AssignedLocation = NormalizeOptional(record.AssignedLocation),
			CustodianName = NormalizeOptional(record.CustodianName),
			ConditionStatus = NormalizeDefault(record.ConditionStatus, "GOOD"),
			LifecycleStatus = NormalizeDefault(record.LifecycleStatus, "ACTIVE"),
			Notes = NormalizeOptional(record.Notes)
		};
	}

	private static ProcurementRequestRecord Sanitize(ProcurementRequestRecord record)
	{
		if (record == null)
		{
			throw new InvalidOperationException("Procurement request is required.");
		}
		decimal num = decimal.Round(record.TotalAmount, 2, MidpointRounding.AwayFromZero);
		if (num <= 0m)
		{
			throw new InvalidOperationException("Total amount must be greater than zero.");
		}
		DateTime dateTime = ((record.RequestDate == default(DateTime)) ? DateTime.Today : record.RequestDate.Date);
		DateTime? neededByDate = record.NeededByDate?.Date;
		if (neededByDate.HasValue && neededByDate.Value < dateTime)
		{
			throw new InvalidOperationException("Needed-by date cannot be earlier than the request date.");
		}
		return new ProcurementRequestRecord
		{
			ProcurementId = record.ProcurementId,
			RequestType = NormalizeOption(record.RequestType, AllowedProcurementTypes, "PROCUREMENT"),
			RequestDate = dateTime,
			NeededByDate = neededByDate,
			RequestTitle = NormalizeRequired(record.RequestTitle, "Request title is required."),
			ProcurementCategory = NormalizeRequired(record.ProcurementCategory, "Procurement category is required."),
			VendorName = NormalizeOptional(record.VendorName),
			RequestedByName = NormalizeOptional(record.RequestedByName),
			TotalAmount = num,
			WorkflowStatus = NormalizeOption(record.WorkflowStatus, AllowedProcurementStatuses, "DRAFT"),
			PurchaseOrderNo = NormalizeOptional(record.PurchaseOrderNo),
			ApprovedByName = NormalizeOptional(record.ApprovedByName),
			ApprovedAt = record.ApprovedAt,
			ItemSummary = NormalizeOptional(record.ItemSummary),
			ApprovalNotes = NormalizeOptional(record.ApprovalNotes),
			Notes = NormalizeOptional(record.Notes)
		};
	}

	private static void ConfigureExpenseParameters(MySqlCommand cmd, ExpenseEntryRecord record)
	{
		cmd.Parameters.AddWithValue("@expenseDate", (object)record.ExpenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
		cmd.Parameters.AddWithValue("@expenseCategory", (object)record.ExpenseCategory);
		cmd.Parameters.AddWithValue("@expenseTitle", (object)record.ExpenseTitle);
		cmd.Parameters.AddWithValue("@payeeName", NormalizeNullable(record.PayeeName));
		cmd.Parameters.AddWithValue("@amount", (object)record.Amount);
		cmd.Parameters.AddWithValue("@paymentMethod", (object)record.PaymentMethod);
		cmd.Parameters.AddWithValue("@status", (object)record.Status);
		cmd.Parameters.AddWithValue("@referenceNo", NormalizeNullable(record.ReferenceNo));
		cmd.Parameters.AddWithValue("@notes", NormalizeNullable(record.Notes));
	}

	private static void ConfigureInventoryParameters(MySqlCommand cmd, InventoryItemRecord record)
	{
		cmd.Parameters.AddWithValue("@itemName", (object)record.ItemName);
		cmd.Parameters.AddWithValue("@category", (object)record.Category);
		cmd.Parameters.AddWithValue("@unit", (object)record.Unit);
		cmd.Parameters.AddWithValue("@quantityOnHand", (object)record.QuantityOnHand);
		cmd.Parameters.AddWithValue("@reorderLevel", (object)record.ReorderLevel);
		cmd.Parameters.AddWithValue("@unitCost", (object)record.UnitCost);
		cmd.Parameters.AddWithValue("@location", NormalizeNullable(record.Location));
		cmd.Parameters.AddWithValue("@itemStatus", (object)record.ItemStatus);
		cmd.Parameters.AddWithValue("@lastRestockedAt", (object)(record.LastRestockedAt.HasValue ? ((IConvertible)record.LastRestockedAt.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : ((IConvertible)DBNull.Value)));
		cmd.Parameters.AddWithValue("@notes", NormalizeNullable(record.Notes));
	}

	private static void ConfigureAssetParameters(MySqlCommand cmd, AssetRecord record)
	{
		cmd.Parameters.AddWithValue("@assetName", (object)record.AssetName);
		cmd.Parameters.AddWithValue("@assetCategory", (object)record.AssetCategory);
		cmd.Parameters.AddWithValue("@assetTag", NormalizeNullable(record.AssetTag));
		cmd.Parameters.AddWithValue("@acquisitionDate", (object)(record.AcquisitionDate.HasValue ? ((IConvertible)record.AcquisitionDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : ((IConvertible)DBNull.Value)));
		cmd.Parameters.AddWithValue("@acquisitionCost", (object)record.AcquisitionCost);
		cmd.Parameters.AddWithValue("@assignedLocation", NormalizeNullable(record.AssignedLocation));
		cmd.Parameters.AddWithValue("@custodianName", NormalizeNullable(record.CustodianName));
		cmd.Parameters.AddWithValue("@conditionStatus", (object)record.ConditionStatus);
		cmd.Parameters.AddWithValue("@lifecycleStatus", (object)record.LifecycleStatus);
		cmd.Parameters.AddWithValue("@notes", NormalizeNullable(record.Notes));
	}

	private static void ConfigureProcurementParameters(MySqlCommand cmd, ProcurementRequestRecord record, string approvedByName, DateTime? approvedAt)
	{
		cmd.Parameters.AddWithValue("@requestType", (object)record.RequestType);
		cmd.Parameters.AddWithValue("@requestDate", (object)record.RequestDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
		cmd.Parameters.AddWithValue("@neededByDate", (object)(record.NeededByDate.HasValue ? ((IConvertible)record.NeededByDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : ((IConvertible)DBNull.Value)));
		cmd.Parameters.AddWithValue("@requestTitle", (object)record.RequestTitle);
		cmd.Parameters.AddWithValue("@procurementCategory", (object)record.ProcurementCategory);
		cmd.Parameters.AddWithValue("@vendorName", NormalizeNullable(record.VendorName));
		cmd.Parameters.AddWithValue("@requestedByName", NormalizeNullable(record.RequestedByName));
		cmd.Parameters.AddWithValue("@totalAmount", (object)record.TotalAmount);
		cmd.Parameters.AddWithValue("@workflowStatus", (object)record.WorkflowStatus);
		cmd.Parameters.AddWithValue("@purchaseOrderNo", NormalizeNullable(record.PurchaseOrderNo));
		cmd.Parameters.AddWithValue("@approvedByName", NormalizeNullable(approvedByName));
		cmd.Parameters.AddWithValue("@approvedAt", NormalizeNullableDateTime(approvedAt));
		cmd.Parameters.AddWithValue("@itemSummary", NormalizeNullable(record.ItemSummary));
		cmd.Parameters.AddWithValue("@approvalNotes", NormalizeNullable(record.ApprovalNotes));
		cmd.Parameters.AddWithValue("@notes", NormalizeNullable(record.Notes));
	}

	private static string NormalizeRequired(string? value, string validationMessage)
	{
		string text = NormalizeOptional(value);
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException(validationMessage);
		}
		return text;
	}

	private static string NormalizeOptional(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return string.Empty;
	}

	private static string NormalizeDefault(string? value, string fallback)
	{
		string text = NormalizeOptional(value);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.ToUpperInvariant();
		}
		return fallback;
	}

	private static string NormalizeOption(string? value, string[] allowedValues, string fallback)
	{
		string b = NormalizeDefault(value, fallback);
		foreach (string text in allowedValues)
		{
			if (string.Equals(text, b, StringComparison.OrdinalIgnoreCase))
			{
				return text.ToUpperInvariant();
			}
		}
		return fallback;
	}

	private static object NormalizeNullable(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Trim();
		}
		return DBNull.Value;
	}

	private static object NormalizeNullableDateTime(DateTime? value)
	{
		if (!value.HasValue)
		{
			return DBNull.Value;
		}
		return value.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
	}

	private static object GetUserIdOrNull()
	{
		if (UserSession.UserId <= 0)
		{
			return DBNull.Value;
		}
		return UserSession.UserId;
	}

	private static int GetBarangayId()
	{
		if (UserSession.BarangayId <= 0)
		{
			return 1;
		}
		return UserSession.BarangayId;
	}

	private async Task<ProcurementRequestRecord?> GetProcurementRequestAsync(int procurementId)
	{
		DataTable dataTable = await DatabaseManagerAsync.LoadTableAsync("\n                SELECT p.procurement_id,\n                       COALESCE(p.request_type, 'PROCUREMENT') AS request_type,\n                       p.request_date,\n                       p.needed_by_date,\n                       p.request_title,\n                       p.procurement_category,\n                       COALESCE(p.vendor_name, '') AS vendor_name,\n                       COALESCE(p.requested_by_name, '') AS requested_by_name,\n                       IFNULL(p.total_amount, 0.00) AS total_amount,\n                       COALESCE(p.workflow_status, 'DRAFT') AS workflow_status,\n                       COALESCE(p.purchase_order_no, '') AS purchase_order_no,\n                       COALESCE(p.approved_by_name, '') AS approved_by_name,\n                       p.approved_at,\n                       COALESCE(p.item_summary, '') AS item_summary,\n                       COALESCE(p.approval_notes, '') AS approval_notes,\n                       COALESCE(p.notes, '') AS notes\n                FROM procurement_request p\n                WHERE p.procurement_id = @procurementId\n                  AND p.barangay_id = @barangayId\n                LIMIT 1", delegate(MySqlCommand cmd)
		{
			cmd.Parameters.AddWithValue("@procurementId", (object)procurementId);
			cmd.Parameters.AddWithValue("@barangayId", (object)GetBarangayId());
		}).ConfigureAwait(continueOnCapturedContext: false);
		return (dataTable.Rows.Count == 0) ? null : MapProcurementRecord(dataTable.Rows[0]);
	}

	private static ProcurementRequestRecord MapProcurementRecord(DataRow row)
	{
		return new ProcurementRequestRecord
		{
			ProcurementId = ((row["procurement_id"] != DBNull.Value) ? Convert.ToInt32(row["procurement_id"], CultureInfo.InvariantCulture) : 0),
			RequestType = (Convert.ToString(row["request_type"], CultureInfo.InvariantCulture)?.Trim() ?? "PROCUREMENT"),
			RequestDate = ReadDateOrDefault(row["request_date"], DateTime.Today),
			NeededByDate = ReadNullableDate(row["needed_by_date"]),
			RequestTitle = (Convert.ToString(row["request_title"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty),
			ProcurementCategory = (Convert.ToString(row["procurement_category"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty),
			VendorName = (Convert.ToString(row["vendor_name"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty),
			RequestedByName = (Convert.ToString(row["requested_by_name"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty),
			TotalAmount = ((row["total_amount"] == DBNull.Value) ? 0m : Convert.ToDecimal(row["total_amount"], CultureInfo.InvariantCulture)),
			WorkflowStatus = (Convert.ToString(row["workflow_status"], CultureInfo.InvariantCulture)?.Trim() ?? "DRAFT"),
			PurchaseOrderNo = (Convert.ToString(row["purchase_order_no"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty),
			ApprovedByName = (Convert.ToString(row["approved_by_name"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty),
			ApprovedAt = ReadNullableDate(row["approved_at"]),
			ItemSummary = (Convert.ToString(row["item_summary"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty),
			ApprovalNotes = (Convert.ToString(row["approval_notes"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty),
			Notes = (Convert.ToString(row["notes"], CultureInfo.InvariantCulture)?.Trim() ?? string.Empty)
		};
	}

	private static DateTime ReadDateOrDefault(object value, DateTime fallback)
	{
		return ReadNullableDate(value) ?? fallback;
	}

	private static DateTime? ReadNullableDate(object value)
	{
		if (value == DBNull.Value)
		{
			return null;
		}
		if (value is DateTime)
		{
			return (DateTime)value;
		}
		if (!DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
		{
			return null;
		}
		return result;
	}

	private static bool ShouldCarryApprovalTrail(string workflowStatus)
	{
		if (!string.Equals(workflowStatus, "APPROVED", StringComparison.OrdinalIgnoreCase) && !string.Equals(workflowStatus, "ORDERED", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(workflowStatus, "RECEIVED", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool ShouldStampApproval(string workflowStatus, ProcurementRequestRecord existing)
	{
		if (ShouldCarryApprovalTrail(workflowStatus) && string.IsNullOrWhiteSpace(existing.ApprovedByName))
		{
			return !existing.ApprovedAt.HasValue;
		}
		return false;
	}

	private static string GetUserDisplayName()
	{
		if (!string.IsNullOrWhiteSpace(UserSession.Username))
		{
			return UserSession.Username.Trim();
		}
		return "Barangay Staff";
	}
}
