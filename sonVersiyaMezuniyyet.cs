using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Xml;
using DocflowGlobal;
using EleWise.ELMA.API;
using EleWise.ELMA.Common.Managers;
using EleWise.ELMA.Documents.Managers;
using EleWise.ELMA.Documents.Models;
using EleWise.ELMA.Documents.Models.Tasks;
using EleWise.ELMA.ElectronicArchive.Models;
using EleWise.ELMA.ElectronicArchive.Services;
using EleWise.ELMA.Extensions;
using EleWise.ELMA.Files;
using EleWise.ELMA.KapitalBank.Docflow.Helpers;
using EleWise.ELMA.KapitalBank.Docflow.Models;
using EleWise.ELMA.KapitalBank.Docflow.Models.Enums;
using EleWise.ELMA.KapitalBank.Docflow.Models.HR;
using EleWise.ELMA.KapitalBank.Docflow.Models.HR.Calendar;
using EleWise.ELMA.KapitalBank.Docflow.Models.HR.Calendar;
using EleWise.ELMA.KapitalBank.Docflow.Models.HR.Vacation;
using EleWise.ELMA.KapitalBank.Docflow.Models.OrganizationStructure;
using EleWise.ELMA.KapitalBank.TLog.Models;
using EleWise.ELMA.Model.Common;
using EleWise.ELMA.Model.Entities;
using EleWise.ELMA.Model.Entities.ProcessContext;
using EleWise.ELMA.Model.Managers;
using EleWise.ELMA.Model.Services;
using EleWise.ELMA.Model.Services;
using EleWise.ELMA.Model.Types.Settings;
using EleWise.ELMA.Runtime.Db.Migrator;
using EleWise.ELMA.Runtime.Db.Migrator.Framework;
using EleWise.ELMA.Runtime.Db.Migrator.Framework;
using EleWise.ELMA.Runtime.Db.Migrator.Framework;
using EleWise.ELMA.Runtime.Db.Migrator.Providers;
using EleWise.ELMA.Runtime.Managers;
using EleWise.ELMA.Scheduling;
using EleWise.ELMA.Security.Managers;
using EleWise.ELMA.Security.Models;
using EleWise.ELMA.Services;
using EleWise.ELMA.Services;
using EleWise.ELMA.Tasks.Models;
using EleWise.ELMA.Workflow.BPMN.Diagrams.Elements;
using EleWise.ELMA.Workflow.BPMN.Diagrams.Elements;
using EleWise.ELMA.Workflow.BPMN.Diagrams.Elements.Connectors;
using EleWise.ELMA.Workflow.Managers;
using EleWise.ELMA.Workflow.Models;
using EleWise.ELMA.Workflow.Services;
using EleWise.ELMA.Workflow.Web.Models;
using NHibernate.Criterion;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Context = EleWise.ELMA.Model.Entities.ProcessContext.P_Vacation;

namespace EleWise.ELMA.Model.Scripts
{
	/// <summary>
	/// Модуль сценариев процесса "Vacation"
	/// </summary>
	/// <example>
	/// <![CDATA[
	/// >>>>>>>>>>>>>>>ВАЖНАЯ ИНФОРМАЦИЯ!!!<<<<<<<<<<<<<<<
	/// Данный редактор создан для работы с PublicAPI.
	/// PublicAPI предназначен для разработки сценариев ELMA.
	/// Например, с помощью PublicAPI можно добавить комментарий к документу:
	/// //Загружаем документ
	/// var doc = PublicAPI.Docflow.Document.Load(56);
	/// //Добавляем комментарий
	/// PublicAPI.Docflow.Document.AddComment(doc, "тут ваш комментарий");
	/// 
	/// Более подробно про PublicAPI вы можете узнать тут: http://www.elma-bpm.ru/kb/article-642ApiRoot.html
	/// 
	/// Если же вам нужна более серьёзная разработка, выходящая за рамки PublicAPI, используйте
	/// сторонние редакторы кода, такие как SharpDevelop и VisualStudio.
	/// Информацию по запуску кода в стороннем редакторе вы можете найти тут:
	/// http://www.elma-bpm.ru/kb/article-837.html
	/// ]]>
	/// </example>
	public partial class P_Vacation_Scripts : EleWise.ELMA.Workflow.Scripts.ProcessScriptBase<Context>
	{
		/// <summary>
		/// AddPositionFilter
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void AddPositionFilter (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			var contactSettings = (EntitySettings)context.GetSettingsFor (c => c.Position);
			contactSettings.FilterQuery = context.EQL_FILTER != null ? context.EQL_FILTER : "Id > 0";
			contactSettings.Save ();
		}

		/// <summary>
		/// GetSelectedUser
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetSelectedUser (Context context)
		{
			if (context.PositionOrgItem != null && context.PositionOrgItem.Parent != null && context.PositionOrgItem.Parent.Parent != null) {
				if (context.PositionOrgItem.Parent.Parent.OrganizationItemType.EnumType == PublicAPI.Enums.KapitalBank.Docflow.Models.Enums.KBDF_OrganizationItemTypesEnum.Curator) {
					context.CuratorApproveBool = true;
				}
				else {
					context.CuratorApproveBool = false;
				}
			}
			//for idare
			if (context.IdaresiOrgItem != null && context.IdaresiOrgItem.OrganizationItemType.EnumType == PublicAPI.Enums.KapitalBank.Docflow.Models.Enums.KBDF_OrganizationItemTypesEnum.Management) {
				if (context.IdaresiOrgItem.OrganizationItemChiefGroup != null && context.IdaresiOrgItem.OrganizationItemChiefGroup.Users.Any () && context.PositionOrgItem.OrganizationItemType.Code != "CHIEF") {
					context.Curators.AddAll (context.IdaresiOrgItem.OrganizationItemChiefGroup.Users);
				}
				if (context.IdaresiOrgItem.OrganizationItemAssistentGroup != null && context.IdaresiOrgItem.OrganizationItemAssistentGroup.Users.Any () && context.PositionOrgItem.OrganizationItemType.Code != "CHIEF") {
					context.Curators.AddAll (context.IdaresiOrgItem.OrganizationItemAssistentGroup.Users);
				}
			}
			if (context.VacationType.Code == "5" || context.VacationType.Code == "9") {
				context.IncludeExceptionalDaysAndWeenends = false;
			}
			context.SubstitutionBool = context.SubstitutionUserEmployee != null ? true : false;
			//			if (context.PositionOrgItem.IsInBranch) {
			//				if (!context.PositionOrgItem.OrganizationItemPosition.HRBCode.Equals ("4065")) {
			//					context.DepChiefs.Clear ();
			//					var structure = context.BranchOrgItem != null ? context.BranchOrgItem : context.DepartmentOrgItem;
			//					context.DepChiefs.AddAll (structure.OrganizationItemChiefGroup.Users);
			//				}
			//			}
			//context.HRSection.AddAll (DocflowGlobal.UserHelper.GetHR_Kadr ());
		}

		public virtual void GetSubstitutionUserInfo (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			SearchOrChangeSubstitutionValue (context, form);
			SubstitutionCheckRequiredReplacement (context, form);
		}

		/// <summary>
		/// AddInitiator
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void AddInitiator (Context context)
		{
			context.UsersFilter.Add (context.Initiator);
		}

		/// <summary>
		/// GetAgreementList
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetAgreementList (Context context)
		{
			//Добавляем сотрудников в список согласующих
			if (context.IsInBranch) {
				var branchDepartmentChief = EntityManager<KBDF_OrganizationItems>.Instance.Find (string.Format ("[Parent] = {0}", context.BranchDepartment.Id)) != null ? EntityManager<KBDF_OrganizationItems>.Instance.Find (string.Format ("[Parent] = {0}", context.BranchDepartment.Id)) : null;
				if (branchDepartmentChief != null && branchDepartmentChief.FirstOrDefault ().OrganizationItemType != null && branchDepartmentChief.FirstOrDefault ().OrganizationItemType.Code == "CHIEF" && branchDepartmentChief.FirstOrDefault ().Users != null)
					context.AggrementList.AddAll (context.DepChiefs);
				if (context.BranchCurator != null && context.BranchCurator.Users != null) {
					context.Curators.Add (context.BranchCurator.Users.FirstOrDefault ());
					context.FinancialDepartmentChiefs.AddAll (context.FinancialDepartment.OrganizationItemChiefGroup.Users);
				}
				if (context.StructureOrgItemType != PublicAPI.Enums.KapitalBank.Docflow.Models.Enums.KBDF_OrganizationItemTypesEnum.Section) {
					context.AggrementList.AddAll (context.OASectionUsers);
				}
			}
			else {
				if (context.SubUsersBool.Value == false && context.ChiefsPosition.IsEmpty == false) {
					context.AggrementList.AddAll (context.ChiefsPosition);
				}
				var departmentChief = EntityManager<KBDF_OrganizationItems>.Instance.Find (string.Format ("[Parent] = {0}", context.Department.Id)) != null ? EntityManager<KBDF_OrganizationItems>.Instance.Find (string.Format ("[Parent] = {0}", context.Department.Id)) : null;
				if (departmentChief != null && departmentChief.FirstOrDefault ().OrganizationItemType != null && departmentChief.FirstOrDefault ().OrganizationItemType.Code == "CHIEF" && departmentChief.FirstOrDefault ().Users != null)
					context.AggrementList.AddAll (departmentChief.FirstOrDefault ().Users);
				if (context.DepartmentCurator != null && context.DepartmentCurator.Users != null) {
					context.Curator = context.DepartmentCurator.Users.FirstOrDefault ();
				}
			}
		}

		/// <summary>
		/// GetApprovementList
		/// </summary>j
		/// <param name="context">Контекст процесса</param>
		public virtual void GetApprovementList (Context context)
		{
			if (context.SubstitutionBool.HasValue && !context.SubstitutionBool.Value) {
				context.Substitution = null;
			}
			var documentApprovementTask = ApprovementResultManager.Instance.Find (string.Format ("Document = {0}", context.VacationStatement.Id));
			foreach (var task in documentApprovementTask) {
				if (task.Item.Results.First ().Status == ApprovalStatus.None)
					continue;
				var newRow = InterfaceActivator.Create<KBDF_Statement_List> ();
				newRow.User = (User)task.Item.User;
				newRow.Comment = task.Item.Comment;
				newRow.Result = task.Item.Results.First ().Status.ToString ();
				newRow.Save ();
				context.VacationStatement.List.Add (newRow);
			}
		}

		/// <summary>
		/// GetDateForApprovmentList
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetDateForApprovmentList (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			if (context.VacationType != null && context.VacationType.Code == "12" && context.CertificationApprover != null) {
				SetAdditionalApprover (context, ApprovalStatus.Confirm);
			}
			context.ProcessName = context.WorkflowInstance.Name;
			if (context.SelectedUserEmployee != null) {
				context.UserPositionName = context.SelectedUserEmployee.FullName + ", " + context.PositionOrgItem.FullName;
			}
			else {
				context.UserPositionName = context.SelectedUser.FullName + ", " + context.PositionOrgItem.FullName;
			}
			SearchOrChangeSubstitutionValue (context, form);
		}

		/// <summary>
		/// SubstitutionalBool
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void SubstitutionalBool (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			if (context.SubUsersBool.HasValue && !context.SubstitutionBool.Value) {
				context.SubstituteOfSubstitutionEmp = null;
				context.SubstitutionUserEmployee = null;
			}
			SearchOrChangeSubstitutionValue (context, form);
			bool hasSubstitution = context.SubstitutionBool.HasValue ? context.SubstitutionBool.Value : false;
			form.For (c => c.SubstitutionUserEmployee).ReadOnly (!hasSubstitution).Required (hasSubstitution).Visible (hasSubstitution);
			form.For (c => c.SubstitutionUserPosition).ReadOnly (true).Required (!hasSubstitution).Visible (hasSubstitution);
			form.For (c => c.SubstituteOfSubstitutionEmp).ReadOnly (!hasSubstitution).Required (context.SubstituteOfSubstitutionEmp != null).Visible (context.SubstituteOfSubstitutionEmp != null);
			form.For (c => c.SubsOfSubstituionPosition).ReadOnly (true).Required (context.SubstituteOfSubstitutionEmp != null).Visible (context.SubstituteOfSubstitutionEmp != null);
			OnChangeSubstitutionCheckAutority (context, form);
		}

		public void SearchOrChangeSubstitutionValue (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			context.Substitution = context.SubstitutionUserEmployee != null ? context.SubstitutionUserEmployee.SystemUser : null;
			context.SubstituteOfSubstitution = context.SubstituteOfSubstitutionEmp != null ? context.SubstituteOfSubstitutionEmp.SystemUser : null;
			context.SubstitutionUserPosition = context.SubstitutionUserEmployee != null ? context.SubstitutionUserEmployee.MainPosition : null;
			context.SubsOfSubstituionPosition = context.SubstituteOfSubstitutionEmp != null ? context.SubstituteOfSubstitutionEmp.MainPosition : null;
		}

		/// <summary>
		/// GetDocVers
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetDocVers (Context context)
		{
			if (context.VacationOrder != null) {
				if (context.VacationOrder.CurrentVersion != null) {
					if (context.VacationOrder.CurrentVersion.File != null) {
						context.VacationOrderVersion = context.VacationOrder.CurrentVersion.File;
					}
				}
			}
		}

		private KBDF_OrganizationItems GetParentOrgitem (KBDF_OrganizationItems orgItem)
		{
			if (orgItem == null) {
				return null;
			}
			var parent = orgItem.Parent;
			while (parent != null && (parent.OrganizationItemType.EnumType != KBDF_OrganizationItemTypesEnum.Government || parent.OrganizationItemType.EnumType != KBDF_OrganizationItemTypesEnum.KapitalBank)) {
				if (parent.OrganizationItemType.EnumType == KBDF_OrganizationItemTypesEnum.Branch) {
					return parent;
				}
				parent = parent.Parent;
			}
			return null;
		}

		private List<KBDF_SubordinationRules> GetRules (KBDF_OrganizationItems orgItem, KBDF_OrganizationItems parent)
		{
			if (orgItem == null) {
				return new List<KBDF_SubordinationRules> ();
			}
			var eql = "";
			if (parent != null) {
				eql = string.Format ("OrganizationItemType = '{0}' AND OrganizationItems = {1}", parent.OrganizationItemType.EnumType.Value.ToString (), parent.Id);
			}
			if (!eql.IsNullOrWhiteSpace ()) {
				eql += " AND ";
			}
			eql += string.Format ("Positions = {0}", orgItem.OrganizationItemPosition.Id);
			return EntityManager<KBDF_SubordinationRules>.Instance.Find (eql).ToList ();
		}

		/// <summary>
		/// Emailblock
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void Emailblock (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			context.UsersEMails.Clear ();
			if (context.UserRecipients.IsEmpty) {
				context.UsersEMails.Clear ();
			}
			else {
				foreach (var usr in context.UserRecipients) {
					var Block = InterfaceActivator.Create<P_Vacation_UsersEMails> ();
					Block.User = usr;
					if (usr.EmailFormat != null) {
						Block.EMail = usr.EmailFormat;
					}
					Block.Save ();
					context.UsersEMails.Add (Block);
				}
			}
		}

		/// <summary>
		/// GetHRUserByBranch
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetHRUserByBranch (Context context)
		{
			if (context.BranchOrgItem != null && context.BranchOrgItem.OrganizationItemCode != null) {
				var Branchs = EntityManager<KPTL_Branch>.Instance.Find (c => c.OrgItemCode == context.BranchOrgItem.OrganizationItemCode);
				if (Branchs.Any ()) {
					var HRUser = Branchs.FirstOrDefault ().HRUser;
					if (HRUser != null) {
						context.HRSection.Clear ();
						context.HRSection.Add (HRUser);
					}
				}
			}
			// get fsied monitoring chief
			if (context.BranchDepartment != null && context.BranchDepartment.HRBCode.Contains ("1104141300") && context.PositionOrgItem != null && context.PositionOrgItem.OrganizationItemType.EnumType == KBDF_OrganizationItemTypesEnum.Chief) {
				var brchief = EntityManager<KBDF_OrganizationItems>.Instance.Find (string.Format ("[Parent] = {0}", context.BranchDepartment.Id)).FirstOrDefault ();
				if (brchief != null && brchief.Users.Any ()) {
					context.DepChiefs.Clear ();
					context.DepChiefs.AddAll (brchief.Users);
				}
			}
		}

		/// <summary>
		/// GetRegNumber
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetRegNumber (Context context)
		{
			if (!context.IsElektronImza) {
				if (string.IsNullOrWhiteSpace (context.OrderRegNumber)) {
					GenerateNumber (context, new DropDownItem ("0", "Gözləmədədir"), context.HREmployee, null, null);
				}
			}
			else {
				DocflowGlobal.DocumentHelper.CreateTracing (context.WorkflowInstance, context.HREmployee);
			}
			context.OrderDate = DateTime.UtcNow.AddHours (4);
			context.OrderRegDate = context.OrderDate.Value.Date.ToString ("dd.MM.yyyy");
			context.DateEndOfTheTaskInHR = DateTime.Now;
			//GetDateEndOfTheTaskInHR (context);
		}

		private void GenerateNumber (Context context, DropDownItem status, User executor, User signer, DateTime? signDate)
		{
			if (!string.IsNullOrWhiteSpace (context.OrderRegNumber)) {
				return;
			}
			var employee = context.SelectedUserEmployee ?? EntityManager<KBDF_HR_BANK_EMPLOYEE>.Instance.Find (string.Format ("SystemUser = {0}", context.SelectedUser.Id)).FirstOrDefault ();
			context.OrderRegNumber = DocflowGlobal.DocumentHelper.GenerateDocumentNumber (context.WorkflowInstance, status, "personal_order", "", " ş", "", executor, signer, signDate, (new[] {
				employee
			}).ToList ());
			context.VacationOrder.RegNumber = context.OrderRegNumber;
			context.OrderDate = DateTime.UtcNow.AddHours (4);
		}

		public virtual void GenerateRegNumber (Context context)
		{
			if (!context.IsElektronImza) {
				GenerateNumber (context, new DropDownItem ("0", "Gözləmədədir"), context.HREmployee, null, null);
			}
		}

		/// <summary>
		/// GetEmailSubjectMessage
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetEmailSubjectMessage (Context context)
		{
			context.MailSubject = "Subject: Məzuniyyət əmri " + context.VacationStatement.VacationType.Name + " " + ((context.VacationStatement.UserEmployee != null) ? context.VacationStatement.UserEmployee.FullName : "");
			if (context.IsInBranch) {
				context.MailSubject = "Əməkdaş: " + ((context.VacationStatement.UserEmployee != null) ? context.VacationStatement.UserEmployee.FullName : "") + " " + "\r\nFilial: " + ((context.BranchOrgItem != null) ? context.BranchOrgItem.FullName : "") + " " + "\r\nƏmrin nömrəsi: " + context.VacationOrder.RegNumber + " " + "\r\nStatus: İcra olundu";
			}
			else {
				context.MailSubject = "Əməkdaş: " + ((context.VacationStatement.UserEmployee != null) ? context.VacationStatement.UserEmployee.FullName : "") + " " + "\r\nDepartament: " + ((context.DepartmentOrgItem != null) ? context.DepartmentOrgItem.FullName : "") + " " + "\r\nƏmrin nömrəsi: " + context.VacationOrder.RegNumber + " " + "\r\nStatus: İcra olundu";
			}
		}

		/// <summary>
		/// GetStartWorkDate
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetStartWorkDate (Context context)
		{
			FindVacationForEmployee (context);
			GetUsedVacDays (context);
			if (!context.VacationType.Code.Equals ("1")) {
				DecreaseVacationDays (context.VacationDaysInfo, context.Days.Value);
			}
			GenerateNewStatementVersion (context);
			DateTime endDate = new DateTime (context.VacationEndDate.Value.Year, context.VacationEndDate.Value.Month, context.VacationEndDate.Value.Day, 23, 59, 59);
			DateTime startDate = new DateTime (context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day, 0, 0, 0);
			if (context.HRAbsence == null) {
				context.HRAbsence = EntityManager<KBDF_HR_Absence>.Instance.Create ();
			}
			context.HRAbsence.Employee = context.SelectedUserEmployee;
			context.HRAbsence.StartDate = startDate;
			context.HRAbsence.EndDate = endDate;
			context.HRAbsence.Reason = context.VacationType.Name;
			context.HRAbsence.ProcessHeader = context.WorkflowInstance.Process.Header;
			context.HRAbsence.IsActive = true;
			context.HRAbsence.Save ();
			if (context.VacationStatement != null && context.StartWork.HasValue) {
				context.VacationStatement.StartWork = context.StartWork;
				context.VacationStatement.Holydays = context.HolidayDays;
			}
			//for Idare
			if ((context.IdaresiChiefPosition != null && context.IdaresiChiefUsers.Any ()) || (context.IdaresiDeputyPosition != null && context.IdaresiDeputyUsers.Any ())) {
				context.Curators.Clear ();
				context.Curators.AddAll (context.IdaresiChiefUsers);
				context.Curators.AddAll (context.IdaresiDeputyUsers);
			}
			context.Header = context.WorkflowInstance.Process.Header;
			context.NoOrgStructureApprovers = context.PositionOrgItem.IsInBranch ? false : true;
		}

		/// <summary>
		/// CreateOrderForm
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void CreateOrderForm (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			bool hasAdditionalAgreement = context.AdditionalAgreementBool != null ? context.AdditionalAgreementBool.Value : false;
			form.For (c => c.AdditionalAgreement).Visible (hasAdditionalAgreement).ReadOnly (!hasAdditionalAgreement).Required (hasAdditionalAgreement);
		}

		/// <summary>
		/// AdditionalAgreementBool
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void AdditionalAgreementBool (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			bool hasAdditionalAgreement = context.AdditionalAgreementBool != null ? context.AdditionalAgreementBool.Value : false;
			form.For (c => c.AdditionalAgreement).Visible (hasAdditionalAgreement).ReadOnly (!hasAdditionalAgreement).Required (hasAdditionalAgreement);
		}

		/// <summary>
		/// ClearAdditionalApprove
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void ClearAdditionalApprove (Context context)
		{
			context.AdditionalAgreementBool = null;
			context.AdditionalAgreement = null;
		}

		/// <summary>
		/// GetApprovementUserList
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetApprovementUserList (Context context)
		{
			try {
				var documentApprovementTask = ApprovementResultManager.Instance.Find (string.Format ("Document = {0}", context.VacationStatement.Id));
				foreach (var task in documentApprovementTask) {
					if (task.Item.Results.First ().Status == ApprovalStatus.None)
						continue;
					context.EmailApprUsers.Add ((User)task.Item.User);
				}
			}
			catch {
			}
			if (!context.EmailApprUsers.IsEmpty) {
				context.EmailUsers.AddAll (context.EmailApprUsers);
			}
			if (context.Initiator != null) {
				context.EmailUsers.Add (context.Initiator);
			}
			if (context.HREmployee != null) {
				context.EmailUsers.Add (context.HREmployee);
			}
			if (context.SelectedUser != null) {
				context.EmailUsers.Add (context.SelectedUser);
			}
			if (context.IsInBranch) {
				if (context.DepartmentChiefPosition != null) {
					bool success = false;
					var users = DocflowGlobal.UserHelper.GetAllUsersInOrgItem (context.DepartmentChiefPosition, false, out success);
					if (success) {
						context.EmailUsers.AddAll (users);
					}
				}
				else {
					if (context.PositionOrgItem.OrganizationItemType.Code == "CHIEF" || context.PositionOrgItem.OrganizationItemType.Code == "DEPUTY") {
						context.EmailUsers.Add (context.Substitution);
					}
				}
			}
			else {
				if (context.PositionOrgItem.OrganizationItemType.Code == "EMPLOYEE") {
					if (context.Department != null && context.Department.OrganizationItemGroup != null) {
						context.EmailUsers.AddAll (context.Department.OrganizationItemGroup.Users);
						//					bool success = false;
						//					var users = DocflowGlobal.UserHelper.GetAllUsersInOrgItem (context.PositionOrgItem.Parent, true, out success);
						//					if (success) {
						//						context.EmailUsers.AddAll (users);
						//					}}
					}
					else
						if (context.SectionOrgItem != null && context.SectionOrgItem.OrganizationItemGroup != null) {
							context.EmailUsers.AddAll (context.SectionOrgItem.OrganizationItemGroup.Users);
						}
				}
				else
					if (context.PositionOrgItem.OrganizationItemType.Code == "CHIEF" || context.PositionOrgItem.OrganizationItemType.Code == "DEPUTY") {
						bool success = false;
						var users = DocflowGlobal.UserHelper.GetAllUsersInOrgItem (context.PositionOrgItem, true, out success);
						if (success) {
							context.EmailUsers.AddAll (users);
						}
					}
			}
			// searching  section users and department chief from their structure user group and adding them to email group
			if (!context.IsOnMaternityLeave && context.TemporaryPosition != null) {
				bool orgItem = false;
				if (!context.TemporaryPosition.IsInBranch) {
					var section = (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.TemporaryPosition, "SECTION", out orgItem);
					if (section != null && section.OrganizationItemGroup != null && section.OrganizationItemGroup.Users.Any ()) {
						context.EmailUsers.AddAll (section.OrganizationItemChiefGroup.Users);
					}
					var dep = (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.TemporaryPosition, "DEPARTMENT", out orgItem);
					if (dep != null && dep.OrganizationItemChiefGroup != null && dep.OrganizationItemChiefGroup.Users.Any ()) {
						context.EmailUsers.AddAll (dep.OrganizationItemChiefGroup.Users);
					}
				}
				else {
					var branch = (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.TemporaryPosition, "BRANCH", out orgItem);
					if (branch != null && branch.OrganizationItemGroup != null && branch.OrganizationItemGroup.Users.Any ()) {
						context.EmailUsers.AddAll (branch.OrganizationItemChiefGroup.Users);
					}
				}
			}
			// for jira plugin
			if (context.Days >= 60) {
				context.JiraTaskName = "Uzunmüddətli məzuniyyət: " + context.SelectedUserEmployee.FullName;
			}
			else {
				context.JiraTaskName = "Məzuniyyət: " + context.SelectedUserEmployee.FullName;
			}
			context.FilesForJira.Add (new EleWise.ELMA.Common.Models.Attachment () {
				File = context.VacationOrderScan
			});
			// jira descriptions
			context.JiraTaskDescription = "Məzuniyyətə çıxan əməkdaş: " + context.SelectedUserEmployee.EmployeeID + " " + context.SelectedUserEmployee.FullName + " ( " + context.SelectedUser.UserName + " ) " + "\r\n" + "Vəzifə: " + context.PositionOrgItem.FullName + "\r\n" + "Məzuniyyətə çıxma tarixləri: " + context.StatementDate.Value.ToString ("dd.MM.yyyy HH:mm") + " - " + context.VacationEndDate.Value.ToString ("dd.MM.yyyy HH:mm") + "\r\n" + "Işə çıxma tarixi: " + context.StartWork.Value.ToString ("dd.MM.yyyy HH:mm") + "\r\n" + "Əvəz edən şəxs: " + ((context.SubstitutionBool == true && context.Substitution != null && context.SubstitutionUserEmployee != null) ? context.SubstitutionUserEmployee.EmployeeID + " " + context.Substitution.FullName + " (" + context.Substitution.UserName + ") " : "Yoxdur") + "\r\n" + "Əvəz edən şəxsin əvəz edicisi: " + ((context.SubstituteOfSubstitution != null && context.SubstituteOfSubstitutionEmp != null) ? context.SubstituteOfSubstitutionEmp.EmployeeID + " " + context.SubstituteOfSubstitutionEmp.FullName + " (" + context.SubstituteOfSubstitution.UserName + ") " : "Yoxdur");
			if (context.RequiredReplacement != null && context.Substitution != null) {
				var subOfSubs = "Əvəz edən şəxsin əvəzedicisi: " + ((context.SubstituteOfSubstitution != null && context.SubstituteOfSubstitutionEmp != null) ? context.SubstituteOfSubstitutionEmp.EmployeeID + " " + context.SubstituteOfSubstitutionEmp.FullName + " (" + context.SubstituteOfSubstitution.UserName + ") " : "Yoxdur") + "\r\n";
				if (context.RequiredReplacement.ReplacementType.Code == "3" && context.SubstituteOfSubstitution != null && context.SubActiveRequiredReplacement != null) {
					subOfSubs = "Əvəz edən şəxsin əvəzedicisi: " + context.SubstituteOfSubstitutionEmp.EmployeeID + " " + context.SubstituteOfSubstitution.FullName + " (" + context.SubstituteOfSubstitution.UserName + ") " + "\r\n" + "İnzibatçılıq dərəcəsi: " + context.SubActiveRequiredReplacement.ReplacementType.Name + "\r\n";
				}
				context.JiraTaskDescription = "Məzuniyyətə çıxan inzibatçı: " + context.SelectedUserEmployee.EmployeeID + " " + context.SelectedUserEmployee.FullName + " ( " + context.SelectedUser.UserName + " ) " + "\r\n" + "Vəzifə: " + context.PositionOrgItem.FullName + "\r\n" + "Əvəz edəcək əməkdaş: " + context.SubstitutionUserEmployee.EmployeeID + " " + context.Substitution.FullName + " ( " + context.Substitution.UserName + " ) " + "\r\n" + "İnzibatçılıq dərəcəsi: " + context.RequiredReplacement.ReplacementType.Name + "\r\n" + subOfSubs + "Məzuniyyətə çıxma tarixləri: " + context.StatementDate.Value.ToString ("dd.MM.yyyy HH:mm") + " - " + context.VacationEndDate.Value.ToString ("dd.MM.yyyy HH:mm") + "\r\n" + "Işə çıxma tarixi: " + context.StartWork.Value.ToString ("dd.MM.yyyy HH:mm");
			}
		}

		/// <summary>
		/// IsCustomApprove
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void IsCustomApprove (Context context)
		{
			
			//начальник независимого отдела
			if (context.PositionOrgItem != null && context.PositionOrgItem.OrganizationItemType != null && context.PositionOrgItem.OrganizationItemType.Code == "CHIEF" && context.PositionOrgItem.Parent != null && context.PositionOrgItem.Parent.OrganizationItemType != null && context.PositionOrgItem.Parent.OrganizationItemType.Code == "SECTION" && context.PositionOrgItem.Parent.Parent != null && context.PositionOrgItem.Parent.Parent.OrganizationItemType != null && context.PositionOrgItem.Parent.Parent.OrganizationItemType.Code == "CURATOR") {
				context.AppUsersList1.Clear ();
				context.AppUsersList2.Clear ();
				context.SelectUsrIsIndependSectChief = true;
				context.SelectedUserInIndependentSection = true;
				context.CustomApprove = true;
				//куратора и помощницу в 1 согласующие
				if (context.PositionOrgItem.Parent.Parent.OrganizationItemAssistentGroup != null && context.PositionOrgItem.Parent.Parent.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
					context.AppUsersList1.AddAll (context.PositionOrgItem.Parent.Parent.OrganizationItemAssistentGroup.Users);
				}
				if (context.PositionOrgItem.Parent.Parent.OrganizationItemChiefGroup != null && context.PositionOrgItem.Parent.Parent.OrganizationItemChiefGroup.Users.IsEmpty == false) {
					context.AppUsersList1.AddAll (context.PositionOrgItem.Parent.Parent.OrganizationItemChiefGroup.Users);
				}
				if (context.PositionOrgItem.Parent.Parent.OrganizationItemGroup != null && context.PositionOrgItem.Parent.Parent.OrganizationItemGroup.Users.IsEmpty == false) {
					context.AppUsersList1.AddAll (context.PositionOrgItem.Parent.Parent.OrganizationItemGroup.Users);
				}
			}
			//сотрудник независимого отдела
			if (context.PositionOrgItem != null && context.PositionOrgItem.Parent != null && context.PositionOrgItem.Parent.OrganizationItemType != null && context.PositionOrgItem.Parent.OrganizationItemType.Code == "CHIEF" && context.PositionOrgItem.Parent.Parent != null && context.PositionOrgItem.Parent.Parent.OrganizationItemType != null && context.PositionOrgItem.Parent.Parent.OrganizationItemType.Code == "SECTION" && context.PositionOrgItem.Parent.Parent.Parent != null && context.PositionOrgItem.Parent.Parent.Parent.OrganizationItemType != null && context.PositionOrgItem.Parent.Parent.Parent.OrganizationItemType.Code == "CURATOR") {
				context.AppUsersList1.Clear ();
				context.AppUsersList2.Clear ();
				context.SelectedUserInIndependentSection = true;
				context.CustomApprove = true;
				//начальника независимого отдела в 1 согласующие
				//добавляем группу чифов независимого отдела в 1 согласующие
				if (context.PositionOrgItem.Parent.Parent.OrganizationItemChiefGroup != null && context.PositionOrgItem.Parent.Parent.OrganizationItemChiefGroup.Users.IsEmpty == false) {
					context.AppUsersList1.AddAll (context.PositionOrgItem.Parent.Parent.OrganizationItemChiefGroup.Users);
				}
				//добаваляем группу юзерс начальника независимого отдела в 1 согласующие
				if (context.PositionOrgItem.Parent.Users.IsEmpty == false) {
					context.AppUsersList1.AddAll (context.PositionOrgItem.Parent.Users);
				}
				//если начальник НО не был найден - отправляем на согласование куратору и его помощнице
				if (context.AppUsersList1.IsEmpty) {
					//куратора и помощницу в 2 согласующие
					if (context.PositionOrgItem.Parent.Parent.Parent.OrganizationItemAssistentGroup != null && context.PositionOrgItem.Parent.Parent.Parent.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
						context.AppUsersList2.AddAll (context.PositionOrgItem.Parent.Parent.Parent.OrganizationItemAssistentGroup.Users);
					}
					if (context.PositionOrgItem.Parent.Parent.Parent.OrganizationItemChiefGroup != null && context.PositionOrgItem.Parent.Parent.Parent.OrganizationItemChiefGroup.Users.IsEmpty == false) {
						context.AppUsersList2.AddAll (context.PositionOrgItem.Parent.Parent.Parent.OrganizationItemChiefGroup.Users);
					}
					if (context.PositionOrgItem.Parent.Parent.Parent.OrganizationItemGroup != null && context.PositionOrgItem.Parent.Parent.Parent.OrganizationItemGroup.Users.IsEmpty == false) {
						context.AppUsersList2.AddAll (context.PositionOrgItem.Parent.Parent.Parent.OrganizationItemGroup.Users);
					}
				}
			}
			//начальник департамента
			if (context.PositionOrgItem != null && context.PositionOrgItem.OrganizationItemType != null && context.PositionOrgItem.OrganizationItemType.Code == "CHIEF" && context.PositionOrgItem.Parent != null && context.PositionOrgItem.Parent.OrganizationItemType != null && context.PositionOrgItem.Parent.OrganizationItemType.Code == "DEPARTMENT") {
				context.AppUsersList1.Clear ();
				context.AppUsersList2.Clear ();
				context.SelUsrIsDepChief = true;
			}
			//куратор
			if (context.PositionOrgItem != null && context.PositionOrgItem.OrganizationItemType != null && context.PositionOrgItem.OrganizationItemType.Code == "CURATOR") {
				context.AppUsersList1.Clear ();
				context.AppUsersList2.Clear ();
				context.SelUsrIsCurator = true;
			}
			//совет правления
			var member1 = EntityManager<KBDF_OrganizationItems>.Instance.Find ("OrganizationItemCode LIKE '102001'").FirstOrDefault ();
			if (member1 != null && member1.Users.IsEmpty == false) {
				var member2 = EntityManager<KBDF_OrganizationItems>.Instance.Find ("OrganizationItemCode LIKE '102000-019'").FirstOrDefault ();
				var member3 = EntityManager<KBDF_OrganizationItems>.Instance.Find ("OrganizationItemCode LIKE '102000-005'").FirstOrDefault ();
				var member4 = EntityManager<KBDF_OrganizationItems>.Instance.Find ("OrganizationItemCode LIKE '102300'").FirstOrDefault ();
				var member5 = EntityManager<KBDF_OrganizationItems>.Instance.Find ("OrganizationItemCode LIKE '102500'").FirstOrDefault ();
				var member6 = EntityManager<KBDF_OrganizationItems>.Instance.Find ("OrganizationItemCode LIKE '102400'").FirstOrDefault ();
				var member7 = EntityManager<KBDF_OrganizationItems>.Instance.Find ("OrganizationItemCode LIKE '102200'").FirstOrDefault ();
				var member8 = EntityManager<KBDF_OrganizationItems>.Instance.Find ("OrganizationItemCode LIKE '102100'").FirstOrDefault ();
				var member9 = EntityManager<KBDF_OrganizationItems>.Instance.Find ("OrganizationItemCode LIKE '102600'").FirstOrDefault ();
				var member10 = EntityManager<KBDF_OrganizationItems>.Instance.Find ("OrganizationItemCode LIKE '102700'").FirstOrDefault ();
				if (member1 != null && member1.Users.IsEmpty == false && member1.Users.Contains (context.SelectedUser)) {
					context.AppUsersList1.Clear ();
					context.AppUsersList2.Clear ();
					context.SelUsrIsBoardMember = true;
					context.CustomApprove = true;
					context.AppUsersList1.AddAll (member1.Users);
					if (member1.OrganizationItemAssistentGroup != null && member1.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemAssistentGroup.Users);
					}
					if (member1.OrganizationItemChiefGroup != null && member1.OrganizationItemChiefGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemChiefGroup.Users);
					}
					if (member1.OrganizationItemGroup != null && member1.OrganizationItemGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemGroup.Users);
					}
				}
				if (member2 != null && member2.Users.IsEmpty == false && member2.Users.Contains (context.SelectedUser)) {
					context.AppUsersList1.Clear ();
					context.AppUsersList2.Clear ();
					context.SelUsrIsBoardMember = true;
					context.CustomApprove = true;
					context.AppUsersList1.AddAll (member1.Users);
					if (member1.OrganizationItemAssistentGroup != null && member1.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemAssistentGroup.Users);
					}
					if (member1.OrganizationItemChiefGroup != null && member1.OrganizationItemChiefGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemChiefGroup.Users);
					}
					if (member1.OrganizationItemGroup != null && member1.OrganizationItemGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemGroup.Users);
					}
				}
				if (member3 != null && member3.Users.IsEmpty == false && member3.Users.Contains (context.SelectedUser)) {
					context.AppUsersList1.Clear ();
					context.AppUsersList2.Clear ();
					context.SelUsrIsBoardMember = true;
					context.CustomApprove = true;
					context.AppUsersList1.AddAll (member1.Users);
					if (member1.OrganizationItemAssistentGroup != null && member1.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemAssistentGroup.Users);
					}
					if (member1.OrganizationItemChiefGroup != null && member1.OrganizationItemChiefGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemChiefGroup.Users);
					}
					if (member1.OrganizationItemGroup != null && member1.OrganizationItemGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemGroup.Users);
					}
				}
				if (member4 != null && member4.Users.IsEmpty == false && member4.Users.Contains (context.SelectedUser)) {
					context.AppUsersList1.Clear ();
					context.AppUsersList2.Clear ();
					context.SelUsrIsBoardMember = true;
					context.CustomApprove = true;
					context.AppUsersList1.AddAll (member1.Users);
					if (member1.OrganizationItemAssistentGroup != null && member1.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemAssistentGroup.Users);
					}
					if (member1.OrganizationItemChiefGroup != null && member1.OrganizationItemChiefGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemChiefGroup.Users);
					}
					if (member1.OrganizationItemGroup != null && member1.OrganizationItemGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemGroup.Users);
					}
				}
				if (member5 != null && member5.Users.IsEmpty == false && member5.Users.Contains (context.SelectedUser)) {
					context.AppUsersList1.Clear ();
					context.AppUsersList2.Clear ();
					context.SelUsrIsBoardMember = true;
					context.CustomApprove = true;
					context.AppUsersList1.AddAll (member1.Users);
					if (member1.OrganizationItemAssistentGroup != null && member1.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemAssistentGroup.Users);
					}
					if (member1.OrganizationItemChiefGroup != null && member1.OrganizationItemChiefGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemChiefGroup.Users);
					}
					if (member1.OrganizationItemGroup != null && member1.OrganizationItemGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemGroup.Users);
					}
				}
				if (member6 != null && member6.Users.IsEmpty == false && member6.Users.Contains (context.SelectedUser)) {
					context.AppUsersList1.Clear ();
					context.AppUsersList2.Clear ();
					context.SelUsrIsBoardMember = true;
					context.CustomApprove = true;
					context.AppUsersList1.AddAll (member1.Users);
					if (member1.OrganizationItemAssistentGroup != null && member1.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemAssistentGroup.Users);
					}
					if (member1.OrganizationItemChiefGroup != null && member1.OrganizationItemChiefGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemChiefGroup.Users);
					}
					if (member1.OrganizationItemGroup != null && member1.OrganizationItemGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemGroup.Users);
					}
				}
				if (member7 != null && member7.Users.IsEmpty == false && member7.Users.Contains (context.SelectedUser)) {
					context.AppUsersList1.Clear ();
					context.AppUsersList2.Clear ();
					context.SelUsrIsBoardMember = true;
					context.CustomApprove = true;
					context.AppUsersList1.AddAll (member1.Users);
					if (member1.OrganizationItemAssistentGroup != null && member1.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemAssistentGroup.Users);
					}
					if (member1.OrganizationItemChiefGroup != null && member1.OrganizationItemChiefGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemChiefGroup.Users);
					}
					if (member1.OrganizationItemGroup != null && member1.OrganizationItemGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemGroup.Users);
					}
				}
				if (member8 != null && member8.Users.IsEmpty == false && member8.Users.Contains (context.SelectedUser)) {
					context.AppUsersList1.Clear ();
					context.AppUsersList2.Clear ();
					context.SelUsrIsBoardMember = true;
					context.CustomApprove = true;
					context.AppUsersList1.AddAll (member1.Users);
					if (member1.OrganizationItemAssistentGroup != null && member1.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemAssistentGroup.Users);
					}
					if (member1.OrganizationItemChiefGroup != null && member1.OrganizationItemChiefGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemChiefGroup.Users);
					}
					if (member1.OrganizationItemGroup != null && member1.OrganizationItemGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemGroup.Users);
					}
				}
				if (member9 != null && member9.Users.IsEmpty == false && member9.Users.Contains (context.SelectedUser)) {
					context.AppUsersList1.Clear ();
					context.AppUsersList2.Clear ();
					context.SelUsrIsBoardMember = true;
					context.CustomApprove = true;
					context.AppUsersList1.AddAll (member1.Users);
					if (member1.OrganizationItemAssistentGroup != null && member1.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemAssistentGroup.Users);
					}
					if (member1.OrganizationItemChiefGroup != null && member1.OrganizationItemChiefGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemChiefGroup.Users);
					}
					if (member1.OrganizationItemGroup != null && member1.OrganizationItemGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemGroup.Users);
					}
				}
				if (member10 != null && member10.Users.IsEmpty == false && member10.Users.Contains (context.SelectedUser)) {
					context.AppUsersList1.Clear ();
					context.AppUsersList2.Clear ();
					context.SelUsrIsBoardMember = true;
					context.CustomApprove = true;
					context.AppUsersList1.AddAll (member1.Users);
					if (member1.OrganizationItemAssistentGroup != null && member1.OrganizationItemAssistentGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemAssistentGroup.Users);
					}
					if (member1.OrganizationItemChiefGroup != null && member1.OrganizationItemChiefGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemChiefGroup.Users);
					}
					if (member1.OrganizationItemGroup != null && member1.OrganizationItemGroup.Users.IsEmpty == false) {
						context.AppUsersList1.AddAll (member1.OrganizationItemGroup.Users);
					}
				}
			}
		}

		/// <summary>
		/// PrepareDateToHRB
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void PrepareDateToHRB (Context context)
		{
			context.HRB_Category = context.VacationType.HRBType;
			if (context.VacationType.HRBHIKType != null) {
				context.HRB_HIKType = context.VacationType.HRBHIKType;
			}
			context.HRBVacationTimeSpan = context.VacationTimeSpan;
			++context.Count;
		}

		/// <summary>
		/// SaveUnicCodeInDocs
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void SaveUnicCodeInDocs (Context context)
		{
			context.VacationStatement.HRBUniqueCode = context.Code;
			context.VacationOrder.HRBUniqueCode = context.Code;
		}

		/// <summary>
		/// SelectedUserIsInitiator
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void SelectedUserIsInitiator (Context context)
		{
			context.SelectedUser = context.Initiator;
		}

		/// <summary>
		/// CheckUserFilter
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckUserFilter (Context context)
		{
			if (context.UsersFilter.Any ()) {
				return true;
			}
			else {
				return false;
			}
		}

		/// <summary>
		/// GetDateOfEntryInHR
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetDateOfEntryInHR (Context context)
		{
			context.DateOfEntryInHR = DateTime.Now;
			//get approvers for eimza
			var approversForEImza = EntityManager<KBDF_ProcessRoutes>.Instance.Find ("Code = 'e_imza'").FirstOrDefault ();
			if (approversForEImza != null) {
				string orgItemsId = String.Join (",", approversForEImza.OrgItems.Select (c => c.Id).ToList ());
				var approversPositions = context.GetSettingsFor (x => x.ApproverEImza) as EntitySettings;
				approversPositions.FilterQuery = "Id in (" + orgItemsId + ") AND IsActive = TRUE";
				approversPositions.Save ();
			}
			if (context.SelectedUser == context.Initiator) {
				GenerateNewStatementVersion (context);
			}
			//Generate Order
			GenerateDocument (context);
			//GenerateNewStatementVersion (context);
		}

		/// <summary>
		/// GetDateEndOfTheTaskInHR
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetDateEndOfTheTaskInHR (Context context)
		{
			var lifeCycleStatus = PublicAPI.Docflow.Objects.LifeCycleStatus.Find ("Name LIKE 'İmtina olunub'").FirstOrDefault ();
			context.VacationStatement.Status = lifeCycleStatus;
			context.VacationStatement.Save ();
			context.DateEndOfTheTaskInHR = DateTime.Now;
			StopAbsence (context);
			if (context.VacationType.Code == "1") {
				ReturnVacationDays (context.VacationDaysInfo, context.Days.Value);
			}
			FindVacationForEmployee (context);
		}

		/// <summary>
		/// SectionChiefCheck
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="GatewayVar"></param>
		public virtual object SectionChiefCheck (Context context)
		{
			var sectionChiefspositions = EntityManager<KBDF_ProcessRoutes>.Instance.Find ("Code = 'rule_chief_dir'").FirstOrDefault ();
			if (sectionChiefspositions != null) {
				if (sectionChiefspositions.Positions.Contains (context.PositionOrgItem.OrganizationItemPosition)) {
					return false;
				}
				else {
					if (context.ChiefsPosition.Any ()) {
						if (context.ChiefsPosition.Contains (context.SelectedUser)) {
							context.ChiefsPosition.Remove (context.SelectedUser);
						}
						return context.ChiefsPosition.Any ();
					}
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// DepDirCheck
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="GatewayVar"></param>
		public virtual object DepDirCheck (Context context)
		{
			var depChiefspositions = EntityManager<KBDF_ProcessRoutes>.Instance.Find ("Code = 'rule_dir_cur'").FirstOrDefault ();
			if (depChiefspositions != null) {
				if (depChiefspositions.Positions.Contains (context.PositionOrgItem.OrganizationItemPosition)) {
					return false;
				}
				else {
					if (context.DepChiefs.Any ()) {
						if (context.DepChiefs.Contains (context.SelectedUser)) {
							context.DepChiefs.Remove (context.SelectedUser);
						}
						else {
							if (!context.DepartmentDeputyUsers.Contains (context.SelectedUser)) {
								context.DepChiefs.AddAll (context.DepartmentDeputyUsers);
							}
						}
						return context.DepChiefs.Any ();
					}
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// CheckSubSection
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckSubSection (Context context)
		{
			if (!context.IsInSubSection || !context.SubSectionChiefs.Any ()) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// CheckIfBranchInitiatorIsSelectedUser
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckIfBranchInitiatorIsSelectedUser (Context context)
		{
			return (context.PositionOrgItem != null && context.PositionOrgItem.OrganizationItemPosition.HRBCode == "4065");
		}

		/// <summary>
		/// ParseXMLToBlock
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void ParseXMLToBlock (Context context)
		{
			if (string.IsNullOrWhiteSpace (context.ResultXMLVacBalance) || !context.ResultXMLVacBalance.Contains ("FULLVACINFOS")) {
				context.FullVacInfo.Add (new P_Vacation_FullVacInfo () {
					StartPeriod = new DateTime (),
					EndPeriod = new DateTime (),
					TotalVacation = 0,
					Used = 0,
					Unused = 0
				});
				return;
			}
			DateTime startPeriod, endPeriod;
			string totalVacStr, totalUsedStr, unusedTotalVacStr;
			double totalVac, unusedTotalVac, totalUsed;
			XmlDocument doc = new XmlDocument ();
			XmlNode node = doc.CreateNode (XmlNodeType.Element, "loadFullVacInfo", "newNode", "");
			node.InnerXml = context.ResultXMLVacBalance;
			XmlNodeList xmlList = node.SelectNodes ("FULLVACINFOS/FULLVACINFO");
			foreach (XmlNode element in xmlList) {
				DateTime.TryParseExact (element.SelectSingleNode ("STARTPERIOD").InnerText.Trim (), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startPeriod);
				DateTime.TryParseExact (element.SelectSingleNode ("ENDPERIOD").InnerText.Trim (), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endPeriod);
				totalVacStr = element.SelectSingleNode ("TOTALVAC").InnerText.Trim ();
				double.TryParse (totalVacStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out totalVac);
				totalUsedStr = element.SelectSingleNode ("TOTALUSED").InnerText.Trim ();
				double.TryParse (totalUsedStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out totalUsed);
				unusedTotalVacStr = element.SelectSingleNode ("UNUSEDTOTALVAC").InnerText.Trim ();
				double.TryParse (unusedTotalVacStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out unusedTotalVac);
				var unUsedVacDays = Convert.ToInt64 (unusedTotalVac);
				if (unUsedVacDays != null || DateTime.Now > startPeriod) {
					var vacInfo = new P_Vacation_FullVacInfo ();
					vacInfo.StartPeriod = startPeriod;
					vacInfo.EndPeriod = endPeriod;
					vacInfo.TotalVacation = Convert.ToInt64 (totalVac);
					vacInfo.Used = Convert.ToInt64 (totalUsed);
					vacInfo.Unused = unUsedVacDays;
					context.FullVacInfo.Add (vacInfo);
				}
			}
		}

		/// <summary>
		/// OnChangeElektronImza
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void OnChangeElektronImza (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			form.For (c => c.ApproverVisa1).Visible (context.IsElektronImza);
			form.For (c => c.ApproverVisa2).Visible (context.IsElektronImza);
			form.For (c => c.ApproverVisa3).Visible (context.IsElektronImza);
			form.For (c => c.ApproverVisa4).Visible (context.IsElektronImza);
			form.For (c => c.ApproverVisa5).Visible (context.IsElektronImza);
			form.For (c => c.ApproverVisa6).Visible (context.IsElektronImza);
			form.For (c => c.ApproverEImza).Visible (context.IsElektronImza).Required (context.IsElektronImza);
			form.For (c => c.NonStandartCase).Visible (context.IsElektronImza);
			form.For (c => c.VacationOrderScan).Visible (!context.IsElektronImza).Required (!context.IsElektronImza);
		}

		/// <summary>
		/// RegisterDocumentFormLoad
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void RegisterDocumentFormLoad (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			if (context.VisaApprovers != null && context.VisaApprovers.Any ()) {
				form.For (c => c.VisaApprovers).Visible (true);
			}
			//form.For (c => c.IsElektronImza).ReadOnly (!string.IsNullOrEmpty (context.OrderRegNumber)).Required (!string.IsNullOrEmpty (context.OrderRegNumber));
			//form.For (c => c.ApproverVisa1).Visible (context.IsElektronImza);
			//form.For (c => c.ApproverVisa2).Visible (context.IsElektronImza);
			//form.For (c => c.ApproverVisa3).Visible (context.IsElektronImza);
			//form.For (c => c.ApproverVisa4).Visible (context.IsElektronImza);
			//form.For (c => c.ApproverVisa5).Visible (context.IsElektronImza);
			//form.For (c => c.ApproverVisa6).Visible (context.IsElektronImza);
			//form.For (c => c.ApproverEImza).Visible (context.IsElektronImza).Required (context.IsElektronImza);
			//form.For (c => c.VacationOrderScan).Visible (!context.IsElektronImza).Required (!context.IsElektronImza);
			var approversForEImza = EntityManager<KBDF_ProcessRoutes>.Instance.Find ("Code = 'e_imza'").FirstOrDefault ();
			if (approversForEImza != null) {
				string orgItemsId = String.Join (",", approversForEImza.OrgItems.Select (c => c.Id).ToList ());
				var approversPositions = context.GetSettingsFor (x => x.ApproverEImza) as EntitySettings;
				approversPositions.FilterQuery = "Id in (" + orgItemsId + ") AND IsActive = TRUE";
				approversPositions.Save ();
			}
			if (!string.IsNullOrEmpty (context.OrderRegNumber)) {
				context.NonStandartCase = true;
				OnChangeNonStandartCase (context, form);
				form.For (c => c.NonStandartCase).ReadOnly (true);
			}
		}

		/// <summary>
		/// GetApproveUser
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetApproveUser (Context context)
		{
			context.NameOfApprovedOrder = context.OrderRegNumber + "+" + context.OrderRegDate + "+" + context.SelectedUserEmployee.EmployeeID;
			string nameoffile = System.IO.Path.GetExtension (context.VacationStatementVersion.Name);
			if (nameoffile == ".doc" || nameoffile == ".docx") {
				string folder = System.IO.Path.GetDirectoryName (context.VacationStatementVersion.ContentFilePath);
				string filename = folder + @"\" + Guid.NewGuid ().ToString () + ".pdf";
				System.IO.File.Copy (context.VacationStatementVersion.ContentFilePath, filename);
				Aspose.Words.Document doc = new Aspose.Words.Document (filename);
				var pdfFile = InterfaceActivator.Create<BinaryFile> ();
				pdfFile.CreateDate = DateTime.Now;
				pdfFile.Name = context.NameOfApprovedOrder + ".pdf";
				pdfFile.InitializeContentFilePath ();
				doc.Save (pdfFile.ContentFilePath, Aspose.Words.SaveFormat.Pdf);
				Locator.GetServiceNotNull<IFileManager> ().SaveFile (pdfFile);
				context.VacationStatementVersion = pdfFile;
			}
			context.EImzaStatus = new DropDownItem ("2", "Təsdiqdə");
			if (!context.NonStandartCase) {
				GenerateDocument (context);
			}
			context.OnlyOnePosition = true;
		}

		/// <summary>
		/// SendToArchive
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void SendToArchive (Context context)
		{
			context.EImzaStatus = context.IsElektronImza ? new DropDownItem ("0", "İmzalanıb") : new DropDownItem ("3", "Imzaya göndərilməyib");
			IKBDF_HR_BANK_EMPLOYEE signer = null;
			IKBDF_HR_BANK_EMPLOYEE verifier = null;
			var attributes = new Dictionary<string, object> ();
			var attachments = new Dictionary<string, BinaryFile> ();
			var type = EntityManager<EA_ArchiveDocumentType>.Instance.Find ("Code = 'vac'").FirstOrDefault ();
			var folders = EntityManager<IEA_Folder>.Instance.Find ("Code ='personal_order'").ToList ();
			var document = context.VacationOrder;
			var file = context.VacationOrderScan;
			var regNo = context.OrderRegNumber;
			var regDate = context.OrderDate;
			long responsibleId = (context.HREmployee != null) ? context.HREmployee.Id : 816;
			var responsible = EntityManager<IKBDF_HR_BANK_EMPLOYEE>.Instance.Find (string.Format ("SystemUser = {0}", responsibleId)).FirstOrDefault ();
			var head = context.IsInBranch ? EA_BasOfisFilial.Branch : EA_BasOfisFilial.BasOfis;
			var categories = EntityManager<IEA_ArchiveDocumentCategory>.Instance.Find ("Code = 'personal_order'").ToList ();
			var employee = context.SelectedUserEmployee ?? EntityManager<IKBDF_HR_BANK_EMPLOYEE>.Instance.Find (string.Format ("SystemUser = {0}", context.SelectedUser.Id)).FirstOrDefault ();
			if (context.IsElektronImza) {
				var signerEmployee = EntityManager<IKBDF_HR_BANK_EMPLOYEE>.Instance.Find (string.Format ("SystemUser = {0}", context.ApproveEImza.Id)).FirstOrDefault ();
				verifier = signerEmployee;
				signer = signerEmployee;
			}
			if (context.SubstitutionUserEmployee != null) {
				attributes.Add ("substitution", context.SubstitutionUserEmployee.FullName);
				attributes.Add ("substitutionCode", context.SubstitutionUserEmployee.EmployeeID);
			}
			attributes.Add ("VacType", context.VacationType.Name);
			attachments.Add ("VacState", context.VacationStatementVersion);
			attachments.Add ("SupDoc", context.PdfApprovementList);
			attachments.Add ("VacOrder", context.VacationOrderScan);
			if (context.SupportingDocuments.Any ()) {
				if (context.SupportingDocuments.Any ()) {
					int i = 1;
					foreach (var element in context.SupportingDocuments) {
						attachments.Add ("app_doc" + i, element.File);
						i++;
					}
				}
			}
			if (!context.IsElektronImza) {
				DocflowGlobal.DocumentHelper.ChangeTrackStatus (context.OrderRegNumber, new DropDownItem ("2", "Əl ilə imzalanıb"));
			}
			DocflowGlobal.ArchiveHelper.AddToArchive (type, regNo, regDate, responsible, file, document, (new[] {
				employee
			}).ToList (), signer, verifier, folders: folders, categories: categories, head: head, attributes: attributes, attachments: attachments);
		}

		/// <summary>
		/// PrepareDocumentContext
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void PrepareDocumentContext (Context context)
		{
			GenerateNumber (context, new DropDownItem ("1", "Elektron imzalanıb"), context.HREmployee, context.ApproveEImza, DateTime.UtcNow.AddHours (4));
			//change status
			context.EImzaStatus = new DropDownItem ("0", "İmzalanıb");
			//new name for order
			context.NameOfApprovedOrder += "+Təstiqedici sənəd";
			//for order
			if (context.EimzaForRequiredReplacement) {
				var archiveService = Locator.GetServiceNotNull<IArchiveDocumentService> ();
				var info = archiveService.GetSignInfo (context.ApprovedDocumentsEImza.FirstOrDefault ().File);
				context.SignDate = DateTime.UtcNow.AddHours (4).ToString ();
				info.Date.Value.ToString ();
				context.Sign = info.Stamp;
			}
			context.OrderDate = DateTime.UtcNow.AddHours (4);
			context.OrderRegDate = context.OrderDate.Value.Date.ToString ("dd.MM.yyyy");
			context.VacationInfo.Status = new DropDownItem ("6", "HRB göndəriləcək");
			context.VacationInfo.OrderDate = context.OrderDate;
			context.VacationInfo.RegistrationNumber = context.OrderRegNumber;
		}

		/// <summary>
		/// CheckSelectedUserPosition
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckSelectedUserPosition (Context context)
		{
			if (!context.IsInBranch) {
				return 1;
			}
			else {
				if (context.PositionOrgItem.OrganizationItemType.Code == "CHIEF" || context.PositionOrgItem.OrganizationItemType.Code == "DEPUTY") {
					return 3;
				}
				return 2;
			}
		}

		/// <summary>
		/// ChangeStatus
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void ChangeStatusEImza (Context context)
		{
			context.EImzaStatus = new DropDownItem ("1", "İmzalanmayıb");
			context.IsElektronImza = false;
		}

		/// <summary>
		/// CheckRequiredReplacement
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void CheckRequiredReplacement (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			context.ReplacementWarning = null;
			context.RequiredReplacement = null;
			if (context.StatementDate != null && context.StartWork != null) {
				string alertMessage = "";
				//muveqqeti axtaris
				context.RequiredReplacement = EntityManager<KBDF_RequiredReplacement>.Instance.Find (string.Format ("User = {0} AND IsActive = TRUE and StartDate < DateTime({1}, {2}, {3}) and EndDate > DateTime({4}, {5}, {6}) and IsTemporary= True", context.SelectedUser.Id, context.StartWork.Value.Year, context.StartWork.Value.Month, context.StartWork.Value.Day, context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day)).FirstOrDefault ();
				if (context.RequiredReplacement != null) {
					alertMessage = "<p style='color:red; font-size:1.3em;'>Seçdiyiniz istifadəçi inzibatçı olduğuna " + "görə əvəzedici şəxs xanası boş olmamalıdır (" + context.RequiredReplacement.ReplacementType.Name + " - " + context.RequiredReplacement.Structure.Name + " - " + context.RequiredReplacement.StartDate.Value.ToString ("dd.MM.yyyy") + "-" + context.RequiredReplacement.EndDate.Value.ToString ("dd.MM.yyyy") + ")</p>";
					context.SubstitutionBool = true;
					form.For (c => c.SubstitutionBool).Visible (true).Required (true).ReadOnly (true);
					form.For (c => c.SubstitutionUserEmployee).Required (true).Visible (true);
					context.ReplacementWarning = new HtmlString (alertMessage);
					form.For (c => c.ReplacementWarning).Visible (true);
				}
				else {
					//daimi axtaris
					context.RequiredReplacement = EntityManager<KBDF_RequiredReplacement>.Instance.Find (string.Format ("User = {0} and IsActive=TRUE and StartDate <= DateTime({1}, {2}, {3}) and EndDate is NULL ", context.SelectedUser.Id, context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day)).FirstOrDefault ();
					if (context.RequiredReplacement != null) {
						alertMessage = "<p style='color:red; font-size:1.3em;'>Seçdiyiniz istifadəçi daimi inzibatçı olduğuna " + "görə əvəzedici şəxs xanası boş olmamalıdır (" + context.RequiredReplacement.ReplacementType.Name + " - " + context.RequiredReplacement.Structure.Name + " - " + context.RequiredReplacement.StartDate.Value.ToString ("dd.MM.yyyy") + "-dən" + ")</p>";
						context.SubstitutionBool = true;
						form.For (c => c.SubstitutionBool).Visible (true).Required (true).ReadOnly (true);
						form.For (c => c.SubstitutionUserEmployee).Required (true).Visible (true);
						context.ReplacementWarning = new HtmlString (alertMessage);
						form.For (c => c.ReplacementWarning).Visible (true);
					}
				}
			}
		}

		public virtual void SubstitutionCheckRequiredReplacement (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			context.ReplacementWarning = null;
			context.SubActiveRequiredReplacement = null;
			CheckRequiredReplacement (context, form);
			if (context.StatementDate != null && context.StartWork != null && context.SubstitutionUserEmployee != null && context.RequiredReplacement != null) {
				string alertMessage = "";
				// evezedici muveqqeti axtaris inzibatci
				context.SubActiveRequiredReplacement = EntityManager<KBDF_RequiredReplacement>.Instance.Find (string.Format ("User = {0} AND IsActive = TRUE and StartDate < DateTime({1}, {2}, {3}) and EndDate > DateTime({4}, {5}, {6}) and IsTemporary= True", context.SubstitutionUserEmployee.SystemUser.Id, context.StartWork.Value.Year, context.StartWork.Value.Month, context.StartWork.Value.Day, context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day)).FirstOrDefault ();
				if (context.SubActiveRequiredReplacement != null) {
					alertMessage = "<p style='color:red; font-size:1.3em;'>Seçdiyiniz əvəzedici şəxs daimi inzibatçı olduğuna " + "görə əvəzedicinin əvəzedicisi xanası boş olmamalıdır (" + context.SubActiveRequiredReplacement.ReplacementType.Name + " - " + context.SubActiveRequiredReplacement.Structure.Name + " - " + context.SubActiveRequiredReplacement.StartDate.Value.ToString ("dd.MM.yyyy") + "-" + context.SubActiveRequiredReplacement.EndDate.Value.ToString ("dd.MM.yyyy") + ")</p>";
					form.For (c => c.SubstituteOfSubstitutionEmp).Required (true).Visible (true);
					context.ReplacementWarning = new HtmlString (alertMessage);
					form.For (c => c.ReplacementWarning).Visible (true);
					return;
				}
				else {
					//evezedici daimi axtaris inzibatci
					context.SubActiveRequiredReplacement = EntityManager<KBDF_RequiredReplacement>.Instance.Find (string.Format ("User = {0} and IsActive=TRUE and StartDate <= DateTime({1}, {2}, {3}) and EndDate is NULL", context.Substitution.Id, context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day)).FirstOrDefault ();
					if (context.SubActiveRequiredReplacement != null) {
						alertMessage = "<p style='color:red; font-size:1.3em;'>Seçdiyiniz əvəzedici şəxs daimi inzibatçı olduğuna " + "görə əvəzedicinin əvəzedicisi xanası boş olmamalıdır (" + context.SubActiveRequiredReplacement.ReplacementType.Name + " - " + context.SubActiveRequiredReplacement.Structure.Name + " - " + context.SubActiveRequiredReplacement.StartDate.Value.ToString ("dd.MM.yyyy") + "-dən" + ")</p>";
						form.For (c => c.SubstituteOfSubstitutionEmp).Required (true).Visible (true);
						context.ReplacementWarning = new HtmlString (alertMessage);
						form.For (c => c.ReplacementWarning).Visible (true);
						return;
					}
				}
			}
		}

		/// <summary>
		/// CheckDate
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckDate (Context context)
		{
			if (context.StartWork.Value < DateTime.Today) {
				return false;
			}
			if (context.RequiredReplacement != null && context.Substitution != null) {
				return true;
			}
			else {
				return false;
			}
		}

		/// <summary>
		/// RemoveReplacement
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void RemoveReplacement (Context context)
		{
			foreach (var requiredReplacement in context.CreatedRequiredReplacements) {
				requiredReplacement.IsActive = false;
				requiredReplacement.Save ();
			}
		}

		/// <summary>
		/// CreateReplacement
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void CreateReplacement (Context context)
		{
			if (context.VacationType.Code == "7" || context.VacationType.Code == "9" || context.VacationType.Code == "5") {
				CreateMaternityLeave (context);
			}
			var vacEndDate = new DateTime (context.VacationEndDate.Value.Year, context.VacationEndDate.Value.Month, context.VacationEndDate.Value.Day, 23, 59, 0);
			CreateAbsecne (context, context.SelectedUser, context.StatementDate.Value, vacEndDate);
			var isCurator = EntityManager<KBDF_CuratorList>.Instance.Find ("Curator = " + context.SelectedUser.Id).FirstOrDefault ();
			if (context.Substitution != null) {
				AddReplacement (context, context.SelectedUser, context.Substitution, context.StatementDate.Value, vacEndDate);
				if (isCurator != null) {
					CreateAbsecne (context, isCurator.Assistant.FirstOrDefault (), context.StatementDate.Value, vacEndDate);
					var isReplacementUserCurator = EntityManager<KBDF_CuratorList>.Instance.Find ("Curator = " + context.Substitution.Id).FirstOrDefault ();
					if (isReplacementUserCurator != null) {
						AddReplacement (context, isCurator.Assistant.FirstOrDefault (), isReplacementUserCurator.Assistant.FirstOrDefault (), context.StatementDate.Value, vacEndDate);
					}
				}
			}
		}

		public virtual void CreateAbsecne (Context context, User user, DateTime startDate, DateTime endDate)
		{
			context.Absence = DocflowGlobal.UserHelper.AddAbsence (user, startDate, endDate, context.VacationType.Name);
		}

		public void AddReplacement (Context context, User replacedUser, User replacementUser, DateTime startDate, DateTime endDate)
		{
			context.Replacement = DocflowGlobal.UserHelper.AddReplacement (replacedUser, replacementUser, startDate, endDate, false, null);
		}

		public virtual void CreateMaternityLeave (Context context)
		{
			var ml = EntityManager<KBDF_MaternityLeave>.Instance.Create ();
			ml.User = context.SelectedUser;
			ml.Employee = context.SelectedUserEmployee;
			ml.Position = context.PositionOrgItem;
			ml.StartDate = context.StartWork;
			ml.EndDate = context.VacationEndDate;
			ml.IsActive = true;
			ml.Save ();
		}

		/// <summary>
		/// EditRegInfo
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void EditRegInfo (Context context)
		{
			context.OrderRegDate = context.OrderDate.Value.ToString ("dd.MM.yyyy");
			if (!context.IsElektronImza) {
				GenerateNumber (context, new DropDownItem ("0", "Gözləmədədir"), context.HREmployee, null, null);
			}
		}

		/// <summary>
		/// OnChangeNonStandartCase
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void OnChangeNonStandartCase (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			form.For (c => c.VacationOrderScan).Required (context.NonStandartCase).ReadOnly (!context.NonStandartCase).Visible (context.NonStandartCase);
		}

		/// <summary>
		/// GenerateDocument
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GenerateDocument (Context context)
		{
			BinaryFile file;
			if (DocflowGlobal.DocumentHelper.GenerateDocumentById (300058, context, out file)) {
				context.VacationOrderScan = file;
			}
		}

		/// <summary>
		/// GenerateNewStatementVersion
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GenerateNewStatementVersion (Context context)
		{
			BinaryFile file;
			context.DocumentCreateDate = DateTime.Now.ToString ("dd.MM.yyyy");
			if (DocflowGlobal.DocumentHelper.GenerateDocumentById (300250, context, out file)) {
				context.VacationStatementVersion = file;
				if (context.VacationStatement != null) {
					PublicAPI.Docflow.DocumentVersion.AddDocumentVersion (context.VacationStatement, context.VacationStatementVersion, PublicAPI.Enums.Documents.DocumentVersionStatus.Current);
				}
			}
		}

		/// <summary>
		/// ConditionForJira
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object ConditionForJira (Context context)
		{
			bool typeCheck = context.VacationType.Code == "9" || context.VacationType.Code == "5";
			if (context.PositionOrgItem.OrganizationItemType.Code != "EMPLOYEE" || typeCheck || context.Days >= 60 || context.RequiredReplacement != null) {
				context.PersonalCode = context.SelectedUserEmployee.EmployeeID;
				context.OfficeType = (context.PositionOrgItem.IsInBranch) ? "BO" : "HO";
				return true;
			}
			return false;
		}

		/// <summary>
		/// GetSubsOfSubstitutionİnfo
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void GetSubsOfSubstitutionİnfo (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			SearchOrChangeSubstitutionValue (context, form);
		}

		/// <summary>
		/// GrantPermissions
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GrantPermissions (Context context)
		{
			var group = EleWise.ELMA.Security.Managers.UserGroupManager.Instance.LoadOrNull (1514);
			//FSHIED
			EleWise.ELMA.Documents.Managers.DmsObjectManager.Instance.AddViewPermission (context.VacationStatement, group);
			//
			context.AuthorityNotification = null;
			if (IsChiefBranchAndSubstitution (context)) {
				GetSubstitutionAuthority (context);
			}
		}

		/// <summary>
		/// ApproveFormLoad
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void ApproveFormLoad (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			if (context.AuthorityNotification != null) {
				form.For (c => c.AuthorityNotification).Visible (true).ReadOnly (true);
			}
			else {
				form.For (c => c.AuthorityNotification).Visible (false).ReadOnly (true);
			}
			if (context.AuthorityInfo.Any ()) {
				form.For (c => c.AuthorityInfo).Visible (true).ReadOnly (true);
				form.For (c => c.IsStartAuthorityProsess).Visible (true).Required (true);
			}
			else {
				form.For (c => c.AuthorityInfo).Visible (false).ReadOnly (true);
				form.For (c => c.IsStartAuthorityProsess).Visible (false).Required (false).ReadOnly (true);
			}
			bool isCertification = context.VacationType != null && context.VacationType.Code == "12";
			form.For (c => c.CertificationField).Visible (isCertification);
			form.For (c => c.CertificationType).Visible (isCertification);
			form.For (x => x.SubstituteOfSubstitutionEmp).Visible (false).Required (false).ReadOnly (false);
			form.For (x => x.SubstitutionBool).Visible (true).Required (true).ReadOnly (false);
			form.For (x => x.SubstitutionUserEmployee).Visible (false).Required (false).ReadOnly (false);
			SubstitutionCheckRequiredReplacement (context, form);
			form.For (x => x.SelectedUserEmployee).Visible (context.SelectedUserEmployee != null).Required (false).ReadOnly (true);
			form.For (x => x.BranchOrgItem).Visible (context.BranchOrgItem != null).Required (false).ReadOnly (true);
			form.For (x => x.DepartmentOrgItem).Visible (context.DepartmentOrgItem != null).Required (false).ReadOnly (true);
			form.For (x => x.SectionOrgItem).Visible (context.SectionOrgItem != null).Required (false).ReadOnly (true);
			form.For (x => x.PositionOrgItem).Visible (context.PositionOrgItem != null).Required (false).ReadOnly (true);
			form.For (x => x.VacationType).Visible (context.VacationType != null).Required (false).ReadOnly (true);
			form.For (x => x.VacationStatementVersion).Visible (context.VacationType != null).Required (false).ReadOnly (true);
			form.For (x => x.StartWork).Visible (context.StartWork != null).Required (false).ReadOnly (true);
			form.For (x => x.ReplacementWarning).Visible (context.ReplacementWarning != null).Required (false).ReadOnly (true);
			form.For (x => x.SupportingDocuments).Visible (context.SupportingDocuments != null).Required (false).ReadOnly (true);
			form.For (x => x.SubSectionOrgItem).Visible (context.SubSectionOrgItem != null).Required (false).ReadOnly (true);
			form.For (x => x.Days).Visible (true).Required (false).ReadOnly (true);
		}

		/// <summary>
		/// GetCertificationApprovers
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetCertificationApprovers (Context context)
		{
			context.TISHUsers.AddAll (DocflowGlobal.UserHelper.GetHR_Education ());
		}

		/// <summary>
		/// CheckCertification
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckCertification (Context context)
		{
			return context.VacationType.Code == "12";
		}

		/// <summary>
		/// GetHRUsers
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetHRUsers (Context context)
		{
			context.SelectedUser = context.Initiator;
			context.SelectedUserEmployee = EntityManager<KBDF_HR_BANK_EMPLOYEE>.Instance.Find (string.Format ("SystemUser = {0}", context.SelectedUser.Id)).FirstOrDefault ();
			//
			if (context.SelectedUserEmployee.TemporaryPosition != null) {
				context.PositionOrgItem = context.SelectedUserEmployee.TemporaryPosition;
				// eger hevale varsa email gondermek ucun
				context.TemporaryPosition = context.SelectedUserEmployee.MainPosition;
			}
			else {
				context.PositionOrgItem = context.SelectedUserEmployee.MainPosition;
			}
			var isInBranch = false;
			KBDF_OrganizationItems tmpItem = context.PositionOrgItem;
			do {
				if (tmpItem.Parent != null && tmpItem.Parent.OrganizationItemType != null) {
					isInBranch = tmpItem.Parent.OrganizationItemType.Code == "BRANCH";
					tmpItem = tmpItem.Parent;
				}
				else {
					break;
				}
			}
			while (!isInBranch);
			context.IsInBranch = isInBranch;
			//
			context.OrganizationItemPosition = context.PositionOrgItem.OrganizationItemPosition;
			//
		}

		/// <summary>
		/// OnChangeMaternityLeave
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void OnChangeMaternityLeave (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			form.For (x => x.MaternityLeave).Visible (context.IsOnMaternityLeave).ReadOnly (!context.IsOnMaternityLeave).Required (context.IsOnMaternityLeave);
			form.For (x => x.SelectedUser).Visible (!context.IsOnMaternityLeave).ReadOnly (context.IsOnMaternityLeave).Required (!context.IsOnMaternityLeave);
			var sett = context.GetSettingsFor (x => x.MaternityLeave) as EntitySettings;
			sett.FilterQuery = string.Format ("IsActive = TRUE");
			sett.Save ();
		}

		/// <summary>
		/// GetMaternityInfo
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetMaternityInfo (Context context)
		{
			context.SelectedUser = context.MaternityLeave.User;
			context.SelectedUserEmployee = context.MaternityLeave.Employee;
			context.PositionOrgItem = context.MaternityLeave.Position;
		}

		/// <summary>
		/// NeedToUseProdCalendar
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual bool NeedToUseProdCalendar (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			context.UseProductionCalendar = context.VacationType.Code == "1" ? true : false;
			context.VacationStatement.StatementDate = context.StatementDate;
			context.VacationStatement.VacationType = context.VacationType;
			context.VacationStatement.Days = context.Days;
			context.VacationTimeSpan = new TimeSpan ((int)context.Days, 0, 0, 0);
			return true;
		}

		/// <summary>
		/// CheckAbsenceForSelectedDate
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckAbsenceForSelectedDate (Context context)
		{
			string vacationStartDay = context.StatementDate.Value.Day.ToString ();
			string vacationStartMonth = context.StatementDate.Value.Month.ToString ();
			string vacationStartYear = context.StatementDate.Value.Year.ToString ();
			string vacationStartDate = string.Format ("DateTime({0}, {1}, {2}, 0, 0)", vacationStartYear, vacationStartMonth, vacationStartDay);
			string vacationEndDay = context.VacationEndDate.Value.Day.ToString ();
			string vacationEndMonth = context.VacationEndDate.Value.Month.ToString ();
			string vacationEndYear = context.VacationEndDate.Value.Year.ToString ();
			string vacationEndDate = string.Format ("DateTime({0}, {1}, {2}, 23, 59)", vacationEndYear, vacationEndMonth, vacationEndDay);
			string firstEql = string.Format ("(StartDate <= {0} AND EndDate >= {0})", vacationStartDate);
			string secondEql = string.Format ("(StartDate <= {0} AND EndDate >= {0})", vacationEndDate);
			string thirdEql = string.Format ("(StartDate >= {0} AND EndDate <= {1})", vacationStartDate, vacationEndDate);
			var eql = string.Format ("({0} OR {1} OR {2}) AND IsActive = TRUE AND Employee = {3}", firstEql, secondEql, thirdEql, context.SelectedUserEmployee.Id);
			var absences = EntityManager<KBDF_HR_Absence>.Instance.Find (eql);
			//var absences = DocflowGlobal.UserHelper.GetAbsence (context.SelectedUser, startDate, endDate);
			context.AbsenceWarning = new HtmlString (absences.Count ().ToString ());
			if (absences != null && absences.Any ()) {
				string absenceinfo = string.Empty;
				foreach (var absence in absences) {
					absenceinfo += string.Format ("{0} {1}-{2}", absence.Reason, absence.StartDate.Value.ToString ("dd-MM-yyyy"), absence.EndDate.Value.ToString ("dd-MM-yyyy")) + Environment.NewLine;
				}
				string warningHtml = "<p style='color:red; font-size:2em;'>Əməkdaş üçün seçimiş tarixlər üzrə məzuniyyət yaradıla bilməz! / Leave cannot be created for an employee on optional dates!" + Environment.NewLine + absenceinfo + "</p>";
				context.AbsenceWarning = new HtmlString (warningHtml);
				// HTML tag-ları sil
        		context.ResultExternal = Regex.Replace(context.AbsenceWarning.ToString(), "<.*?>", string.Empty);
				return true;
			}
			return false;
		}

		/// <summary>
		/// CheckLdapBlock
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckLdapBlock (Context context)
		{
			return context.Days >= 60 || context.VacationType.Code == "9" || context.VacationType.Code == "5";
		}

		/// <summary>
		/// ChangeResponsible
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void ChangeResponsible (Context context)
		{
			var provider = Locator.GetServiceNotNull<ITransformationProvider> ();
			var query = @"update WorkflowInstance set Responsible = 1 where Id = @p1";
			provider.ExecuteNonQuery (query, new Dictionary<string, object> {
				{
					"p1",
					context.WorkflowInstance.Id
				}
			});
		}

		public void SetAdditionalApprover (Context context, ApprovalStatus status)
		{
			DocflowGlobal.ApprovementHelper.SetApprovementState (context.VacationStatement, status, context.CertificationApprover, context.CertificationApproverComment);
		}

		public void SetApproveData (Context context, bool approved)
		{
			if (context.CertificationApprover != null) {
				var newRow = InterfaceActivator.Create<KBDF_Statement_List> ();
				newRow.User = context.CertificationApprover;
				newRow.Comment = string.IsNullOrEmpty (context.CertificationApproverComment) ? "+" : context.CertificationApproverComment;
				newRow.Result = approved ? ApprovalStatus.Confirm.ToString () : ApprovalStatus.Unconfirm.ToString ();
				newRow.Save ();
				context.VacationStatement.List.Add (newRow);
			}
		}

		/// <summary>
		/// Approve
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual bool Approve (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			SetApproveData (context, true);
			return true;
		}

		/// <summary>
		/// Reject
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual bool Reject (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			form.For (c => c.CertificationApproverComment).Required (true);
			if (string.IsNullOrEmpty (context.CertificationApproverComment)) {
				return false;
			}
			SetApproveData (context, false);
			return true;
		}

		/// <summary>
		/// DateEditFormLoad
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void DateEditFormLoad (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			bool isCertification = context.VacationType != null && context.VacationType.Code == "12";
			if (isCertification) {
				context.Days = 2;
				form.For (c => c.Days).ReadOnly (isCertification);
			}
		}

		/// <summary>
		/// SetApprover
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void SetApprover (Context context)
		{
			if (context.CertificationApprover != null) {
				SetAdditionalApprover (context, ApprovalStatus.Unconfirm);
			}
		}

		private User GetLastExecutor (Context context, User user)
		{
			var tasksFilter = InterfaceActivator.Create<IWorkflowTaskBaseFilter> ();
			tasksFilter.InstanceId = context.WorkflowInstance.Id;
			tasksFilter.DisableSecurity = true;
			tasksFilter.Status = TaskBaseStatus.Complete;
			var lastTask = WorkflowTaskBaseManager.Instance.Find (tasksFilter, null).ToList ().LastOrDefault ();
			if (lastTask != null) {
				user = (User)lastTask.Executor;
			}
			return user;
		}

		/// <summary>
		/// StopAbsence
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void StopAbsence (Context context)
		{
			context.HRAbsence.IsActive = false;
			context.HRAbsence.Save ();
		}

		/// <summary>
		/// CheckSignType
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual bool CheckSignType (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			return !context.IsElektronImza && string.IsNullOrEmpty (context.OrderRegNumber);
		}

		/// <summary>
		/// CheckFields
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual bool CheckFields (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			return !string.IsNullOrEmpty (context.OrderRegNumber) || context.IsElektronImza;
		}

		/// <summary>
		/// VacTypeDynamic
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void VacTypeDynamic (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			context.StartWork = null;
			context.VacationEndDate = null;
			context.Days = null;
			context.VacationTotalCount = 0;
			context.UseProductionCalendar = context.VacationType.Code == "1" ? true : false;
			context.Komment = new List<String> {
				"1",
				"3",
				"8",
				"13"
			}.Contains (context.VacationType.Code) ? "ndən" : "dən";
			try {
				ChangeVacationTypeDynamic (context, form);
				CalculateStartWorkDay (context, form);
				CheckRequiredReplacement (context, form);
				SubstitutionCheckRequiredReplacement (context, form);
				FindVacationForEmployee (context);
				GetUsedVacDays (context);
				CalculateMustUseVacDaysCount (context);
			}
			catch (Exception ex) {
				context.VacationAlert = new HtmlString (ex.StackTrace);
				form.For (c => c.VacationAlert).ReadOnly (true).Required (false).Visible (true);
			}
		}

		public void ChangeVacationTypeDynamic (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			if (context.VacationType.Code == "6") {
				form.For (c => c.Days).ReadOnly (true).Required (false).Visible (true);
				context.Days = 2;
			}
			else {
				context.MarriageDays = null;
				form.For (c => c.MarriageDays).ReadOnly (true).Required (false).Visible (false);
				form.For (c => c.Days).ReadOnly (false).Required (true).Visible (true);
				if (context.VacationType.Code == "5" || context.VacationType.Code == "9") {
					context.IncludeExceptionalDaysAndWeenends = false;
					form.For (c => c.Days).ReadOnly (true);
					form.For (c => c.VacationEndDate).ReadOnly (false).Required (true).Visible (true);
				}
				else {
					form.For (c => c.Days).ReadOnly (false);
					context.IncludeExceptionalDaysAndWeenends = true;
					context.VacationEndDate = null;
					form.For (c => c.VacationEndDate).ReadOnly (true).Required (false).Visible (false);
				}
			}
			form.For (x => x.DogDate).Visible (context.VacationType.Code == "9").Required (false).ReadOnly (false);
			form.For (x => x.MedicalCertificateNumber).Visible (context.VacationType.Code == "9").Required (false).ReadOnly (false);
			if (context.VacationType.Code == "12") {
				form.For (c => c.CertificationField).Visible (true).Required (true);
				context.Days = 2;
				form.For (x => x.Days).ReadOnly (true);
				//GetStartDate (context, form);
			}
			else {
				//form.For (x => x.Days).ReadOnly (false);
				form.For (c => c.CertificationField).Visible (false).Required (false);
				form.For (c => c.CertificationType).Visible (false).Required (false);
				context.CertificationField = null;
				context.CertificationType = null;
			}
			if (context.VacationType.Code == "8") {
				form.For (c => c.Days).Visible (true).ReadOnly (true);
				context.Days = 1;
			}
			if (context.VacationType.Code == "7") {
				form.For (c => c.Days).Visible (true).ReadOnly (true);
				context.Days = 3;
			}
			var settingz = (DateTimeSettings)context.GetSettingsFor (c => c.StatementDate);
			if (context.VacationType != null && context.VacationType.Code == "1") {
				if (!context.HRSection.Contains (context.Initiator)) {
					settingz.MinDateValue = DateTime.Today.AddMonths (-1);
				}
				form.For (x => x.PaymentStatus).Visible (true).Required (true).ReadOnly (false);
			}
			else {
				context.UseProductionCalendar = false;
				settingz.MinDateValue = new DateTime ();
				form.For (x => x.PaymentStatus).Visible (false).Required (false).ReadOnly (true);
			}
			form.For (x => x.SupportingDocuments).Visible (true).ReadOnly (false).Required (new List<string> {
				"3",
				"5",
				"9",
				"12",
				"13"
			}.Contains (context.VacationType.Code) || context.SelectedUser != context.Initiator);
			settingz.Save ();
			form.For (x => x.VacationTotalCount).Visible (context.VacationType.Code.Equals ("1")).ReadOnly (true);
		}

		/// <summary>
		/// CertificationFieldChange
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void CertificationFieldChange (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			form.For (c => c.CertificationType).Visible (true).Required (true);
			var settings = (EntitySettings)context.GetSettingsFor (e => e.CertificationType);
			settings.FilterQuery = "Field = " + context.CertificationField.Id;
			settings.Save ();
			if (context.StatementDate != null && context.StartWork != null) {
				GenerateNewStatementVersion (context);
			}
		}

		/// <summary>
		/// StartForm
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void StartForm (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			var typeIds = EntityManager<KBDF_HR_VacationType>.Instance.FindAll ().OrderBy (x => x.Id).Select (x => x.Id).ToList ();
			var contactSettings = (EntitySettings)context.GetSettingsFor (c => c.VacationType);
			contactSettings.FilterQuery = string.Format ("Id in ({0})", string.Join (",", typeIds));
			contactSettings.Save ();
			form.For (c => c.VacationAlert).Visible (false);
			form.For (c => c.Notification).Visible (false);
			bool isCertification = context.VacationType != null && context.VacationType.Code == "12";
			form.For (c => c.MarriageDays).ReadOnly (true).Required (false).Visible (false);
			form.For (c => c.VacationEndDate).ReadOnly (true).Required (false).Visible (false);
			form.For (c => c.Days).ReadOnly (isCertification).Required (true).Visible (true);
			form.For (c => c.DogDate).ReadOnly (false).Required (false).Visible (false);
			form.For (c => c.CertificationField).Visible (isCertification).Required (isCertification);
			form.For (c => c.CertificationType).Visible (isCertification).Required (isCertification);
			form.For (c => c.DogDate).ReadOnly (false).Required (false).Visible (false);
			form.For (c => c.DxshahaddetDate).ReadOnly (false).Required (false).Visible (false);
			//if (context.BranchOrgItem == null) {
			form.For (c => c.BranchOrgItem).ReadOnly (true).Required (false).Visible (false);
			//}
			if (context.DepartmentOrgItem == null) {
				form.For (c => c.DepartmentOrgItem).ReadOnly (true).Required (false).Visible (false);
			}
			if (context.SectionOrgItem == null) {
				form.For (c => c.SectionOrgItem).ReadOnly (true).Required (false).Visible (false);
			}
			if (context.PositionOrgItem == null) {
				form.For (c => c.PositionOrgItem).ReadOnly (true).Required (false).Visible (false);
			}
			form.For (c => c.SupportingDocuments).Required (isCertification || context.SelectedUser != context.Initiator);
			form.For (x => x.SubstitutionUserEmployee).Visible (CheckSubstitutionRequired (context)).Required (false).ReadOnly (false);
			form.For (x => x.SubstituteOfSubstitutionEmp).Visible (false).Required (false).ReadOnly (false);
			form.For (x => x.VacationStatementVersion).Visible (context.VacationType != null && context.VacationType.Code.Equals ("1")).ReadOnly (true);
			form.For (x => x.PaymentStatus).Visible (context.VacationType != null && context.VacationType.Code.Equals ("1")).Required (context.VacationType != null && context.VacationType.Code.Equals ("1")).ReadOnly (false);
			form.For (x => x.FullVacInfo).Visible (context.StatementDate != null && context.Days.HasValue).ReadOnly (true).Required (false);
			form.For (x => x.MedicalCertificateNumber).Visible (context.VacationType != null && context.VacationType.Code.Equals ("9")).ReadOnly (false);
			CalculateMustUseVacDaysCount (context);
		}

		public bool CheckSubstitutionRequired (Context context)
		{
			var posForSub = EntityManager<KBDF_ProcessRoutes>.Instance.Find ("Code = 'vac_substitution'").FirstOrDefault ();
			var position = context.SelectedUserEmployee.TemporaryPosition != null ? context.SelectedUserEmployee.TemporaryPosition.OrganizationItemPosition : context.SelectedUserEmployee.MainPosition.OrganizationItemPosition;
			if (posForSub != null && posForSub.Positions.Contains (position)) {
				return true;
			}
			return false;
		}

		/// <summary>
		/// GetStartDate
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void WarningWeekends (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			form.For (c => c.VacationAlert).Visible (false);
			if (context.VacationType != null && context.VacationType.Code == "12") {
				context.Days = 2;
				form.For (x => x.Days).ReadOnly (true);
			}
			if (context.VacationType != null && (context.VacationType.Code == "5" || context.VacationType.Code == "9")) {
				return;
			}
			var ExceptionDays = EntityManager<KDBF_ExceptionalDays>.Instance.FindAll ();
			var statementDateMonth = context.StatementDate.Value.Month;
			var statementDateYear = context.StatementDate.Value.Year;
			foreach (var day in ExceptionDays) {
				if (day.StartDate.Value.Month == statementDateMonth && day.StartDate.Value.Year == statementDateYear) {
					TimeSpan startDiff = new DateTime (day.StartDate.Value.Year, day.StartDate.Value.Month, day.StartDate.Value.Day) - context.StatementDate.Value;
					TimeSpan endDiff = new DateTime (day.EndDate.Value.Year, day.EndDate.Value.Month, day.EndDate.Value.Day) - context.StatementDate.Value;
					if (startDiff.Days <= 0 && endDiff.Days >= 0) {
						string alertMessage = "<p style='color:red; font-size:1.5em;'>Qeyri iş günlərində məzuniyyət yaradıla bilməz. / Leave cannot be created on non-working days.</p>";
						context.VacationAlert = new HtmlString (alertMessage);
						form.For (c => c.VacationAlert).ReadOnly (true).Required (false).Visible (true);
						context.StartWork = null;
						context.StatementDate = null;
						return;
					}
				}
			}
			if (context.StatementDate != null) {
				var prodCal = Locator.GetServiceNotNull<IProductionCalendarService> ();
				bool isWorkDay = prodCal.IsWorkDay (context.StatementDate.Value);
				if (!isWorkDay) {
					string alertMessage = "<p style='color:red; font-size:1.5em;'>Qeyri iş günlərində məzuniyyət yaradıla bilməz. / Leave cannot be created on non-working days.</p>";
					context.VacationAlert = new HtmlString (alertMessage);
					form.For (c => c.VacationAlert).ReadOnly (true).Required (false).Visible (true);
					context.StartWork = null;
					context.StatementDate = null;
					return;
				}
			}
			/*if (context.StatementDate != null && ((int)context.StatementDate.Value.DayOfWeek == 6 || (int)context.StatementDate.Value.DayOfWeek == 0))
				{
					context.StatementDate = null;
					string alertMessage = "<p style='color:red; font-size:2em;'>Qeyri iş günləri məzuniyyət yaratmaq olmaz!</p>";
					context.VacationAlert = new HtmlString(alertMessage);
					form.For(c => c.VacationAlert).ReadOnly(true).Required(false).Visible(true);
					
					
				}*/}

		/// <summary>
		/// CheckAprrovers
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckAprrovers (Context context)
		{
			var Curator = EntityManager<KBDF_ProcessRoutes>.Instance.Find ("Code = 'Mail_delivery_access'").FirstOrDefault ();
			if (Curator != null && Curator.Positions.Contains (context.PositionOrgItem.OrganizationItemPosition)) {
				return true;
			}
			else {
				return false;
			}
		}

		/// <summary>
		/// DeleteApprovementList
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void DeleteApprovementList (Context context)
		{
			if (context.VacationType.Code != "12" && !context.IsInBranch) {
				context.PdfApprovementList = null;
			}
		}

		/// <summary>
		/// GetMailSubject
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetMailSubject (Context context)
		{
			var approver = DocflowGlobal.ApprovementHelper.GetFirstApprover (context.PositionOrgItem);
			if (approver.Position == context.PositionOrgItem) {
				approver = DocflowGlobal.ApprovementHelper.GetFirstApprover (context.DepartmentOrgItem);
			}
			if (approver != null && approver.Approvers.Any ()) {
				context.MailNotificationUser.AddAll (approver.Approvers);
			}
			var IsaAgile = EntityManager<KBDF_ProcessRoutes>.Instance.Find ("Code = 'isagile'").FirstOrDefault ();
			if (IsaAgile != null && IsaAgile.Positions.Contains (context.PositionOrgItem.OrganizationItemPosition)) {
				if (context.TemporaryPosition != null && !context.TemporaryPosition.IsInBranch) {
					var PO = EntityManager<KBDF_OrganizationItems>.Instance.Find (string.Format ("HRBCode ='{0}' AND OrganizationItemPosition = 541", context.TemporaryPosition.HRBCode)).FirstOrDefault ();
					if (PO != null && PO.Users.Any ()) {
						context.MailNotificationUser.AddAll (PO.Users);
					}
					bool orgItem = false;
					var dep = (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.TemporaryPosition, "DEPARTMENT", out orgItem);
					if (dep != null && dep.OrganizationItemChiefGroup != null && dep.OrganizationItemChiefGroup.Users.Any ()) {
						context.MailNotificationUser.AddAll (dep.OrganizationItemChiefGroup.Users);
					}
				}
				if (context.ChiefsPosition != null) {
					context.MailNotificationUser.AddAll (context.ChiefsPosition);
				}
			}
			context.SubjectForMail = "Məzuniyyət prosesinin başladılması";
			context.MailText = "Məlumat üçün bildiririk ki," + " " + ((context.PositionOrgItem.FullName != null) ? context.PositionOrgItem.FullName : context.PositionOrgItem.Name) + " , " + context.SelectedUserEmployee.FullName + " " + context.StatementDate.Value.ToString ("dd.MM.yyyy") + " tarixdən" + " " + context.Days + " " + "gün müddətinə" + " " + context.VacationType.Name + " " + "ilə bağlı" + " " + context.WorkflowInstance.StartDate.Value.ToString ("dd.MM.yyyy") + " " + "tarixdə ƏMİEŞ-ə müraciət etmişdir." + Environment.NewLine + Environment.NewLine + "Personal Kod:" + context.SelectedUserEmployee.EmployeeID + Environment.NewLine + "Ad,soyad və ata adı:" + context.SelectedUserEmployee.FullName + Environment.NewLine + "Məzuniyyətin növü:" + context.VacationType.Name + Environment.NewLine + "Məzuniyyətin başlama tarixi:" + context.StatementDate.Value.ToString ("dd.MM.yyyy") + Environment.NewLine + "Məzuniyyətin bitmə tarixi:" + context.VacationEndDate.Value.ToString ("dd.MM.yyyy") + Environment.NewLine + "İşə başlama tarixi:" + context.StartWork.Value.ToString ("dd.MM.yyyy") + Environment.NewLine + "Məzuniyyətin müddəti:" + context.Days;
		}

		/// <summary>
		/// GetStartWorkDate_OnChange
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void Days_OnChange (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			form.For (c => c.Substitution).Required (false);
			form.For (c => c.SubstituteOfSubstitution).Required (false);
			FindVacationForEmployee (context);
			GetUsedVacDays (context);
			CalculateStartWorkDay (context, form);
			SubstitutionCheckRequiredReplacement (context, form);
			CalculateAvailableVacDays (context);
			CheckVacationRule (context, form);
		}

		public void CheckVacationRule (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			string notificationText = "";
			if (context.VacationType.Code.Equals ("1")) {
				var vacCount = context.FullVacInfo.Where (x => x.StartPeriod <= context.StatementDate).Select (x => x.Unused).Sum () - context.Days;
				if (vacCount < 0) {
					context.Days = 0;
					context.StatementDate = null;
					context.StartWork = null;
					notificationText = "İstifadə etmək istədiyiniz məzuniyyətin gün sayı cari limitinizdən artıqdır!" + Environment.NewLine + "The number of vacation days you want to use exceeds your current limit!";
					context.VacationAlert = new HtmlString (string.Format ("<p style='color:red; font-size:1.3em;'> {0}</p>", notificationText));
					form.For (x => x.VacationAlert).Visible (true).ReadOnly (true);
				}
				else
					if (context.CurrentAvailableVacDays < 0) {
						notificationText = string.Format ("Sizin cari günə formalaşmış {0} gün məzuniyyətiniz var.{1} gün avans məzuniyyət istifadə edəcəksiniz.", 0, context.Days) + Environment.NewLine + string.Format ("You currently have {0} days of accrued leave. You will use {1} days of advance leave.", 0, context.Days);
						context.VacationAlert = new HtmlString (string.Format ("<p style='color:red; font-size:1.3em;'> {0} </p>", notificationText));
						form.For (x => x.VacationAlert).Visible (true).ReadOnly (true);
					}
					else
						if (context.Days > context.CurrentAvailableVacDays) {
							notificationText = string.Format ("Sizin cari günə formalaşmış {0} gün məzuniyyətiniz var.{1} gün avans məzuniyyət istifadə edəcəksiniz.", context.CurrentAvailableVacDays, context.Days - context.CurrentAvailableVacDays) + Environment.NewLine + string.Format ("You currently have {0} days of accrued leave. You will use {1} days of advance leave.", context.CurrentAvailableVacDays, context.Days - context.CurrentAvailableVacDays);
							context.VacationAlert = new HtmlString (string.Format ("<p style='color:red; font-size:1.3em;'> {0} </p>", notificationText));
							form.For (x => x.VacationAlert).Visible (true).ReadOnly (true);
						}
						else {
							context.VacationAlert = null;
							form.For (x => x.VacationAlert).Visible (false).ReadOnly (true);
						}
			}
		}

		public void CalculateStartWorkDay (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			context.VacationTimeSpan = null;
			context.StartWork = null;
			if ((context.Days == null || context.StatementDate == null || context.VacationType == null) && context.IncludeExceptionalDaysAndWeenends != false) {
				return;
			}
			if (context.Days.HasValue) {
				if (context.Days <= 0) {
					context.Days = null;
					return;
				}
				context.VacationTimeSpan = new TimeSpan ((int)context.Days, 0, 0, 0);
			}
			if (context.IncludeExceptionalDaysAndWeenends == false && context.VacationEndDate != null) {
				if (context.VacationEndDate < context.StatementDate) {
					context.VacationEndDate = null;
					context.Days = null;
					return;
				}
				TimeSpan remaindate;
				DateTime d1 = context.VacationEndDate.Value;
				DateTime d2 = context.StatementDate.Value;
				remaindate = d1 - d2;
				var DaysTMP = (long)remaindate.Days;
				context.Days = DaysTMP + 1;
				context.VacationTimeSpan = new TimeSpan ((int)context.Days, 0, 0, 0);
				context.VacationEndDate = context.StatementDate.Value.AddDays (-1) + context.VacationTimeSpan.Value;
				context.StartWork = context.VacationEndDate.Value.AddDays (1);
			}
			else
				if (context.UseProductionCalendar) {
					context.VacationEndDate = null;
					GetHolidayDatesFromObject (context);
				}
				else {
					try {
						//parameters.Success = true;
						var prodCal = Locator.GetServiceNotNull<IProductionCalendarService> ();
						if (context.StatementDate.HasValue && context.VacationTimeSpan.HasValue) {
							context.VacationEndDate = context.StatementDate.Value + context.VacationTimeSpan.Value;
							var evalDate = context.UseProductionCalendar ? prodCal.EvalTargetTime (context.StatementDate.Value, context.VacationTimeSpan.Value) : context.VacationEndDate.Value;
							var delta = 1;
							if (evalDate.Ticks > context.VacationEndDate.Value.Ticks) {
								context.StartWork = evalDate;
								delta = -1;
							}
							else {
								//						parameters.PrevEndDateWorkDays = evalDate;
							}
							var isWorkDay = prodCal.IsWorkDay (context.VacationEndDate.Value);
							var workDate = isWorkDay ? evalDate : context.VacationEndDate.Value;
							while (!isWorkDay) {
								workDate = workDate.AddDays (delta);
								isWorkDay = prodCal.IsWorkDay (workDate);
							}
							var wTime = prodCal.GetWorkTimeStart (workDate);
							if (delta == 1) {
								//wTime = prodCal.GetWorkTimeStart(workDate);
								context.StartWork = new DateTime (workDate.Year, workDate.Month, workDate.Day, wTime.Hours, wTime.Minutes, wTime.Seconds);
							}
							context.VacationEndDate = context.VacationEndDate.Value.AddDays (-1);
						}
					}
					catch (Exception ex) {
						//	parameters.ErrorMessage = ex.Message;
						//	parameters.Success = false;
					}
				}
			GenerateNewStatementVersion (context);
			string notificationAze = "";
			string notificationEng = "";
			if (context.Initiator != context.SelectedUserEmployee.SystemUser && context.StartWork.HasValue) {
				notificationAze += "Sizə təqdim olunmuş ərizəni çap etdikdən sonra müvafiq hissədən məzuniyyətə çıxacaq əməkdaşın imzalamasını təmin edin." + "Daha sonra imzalı sənədi  skan edin və təsdiqedici sənəd bölməsinə yükləyin.";
				notificationEng += "After printing the application submitted to you, ensure the signature of the employee who will be on leave from the relevant department." + "Then scan the signed document  and upload it to the confirmation document section.";
			}
			switch (context.VacationType.Code) {
			case "3":
				notificationAze += " Təhsil haqqında arayış sənədini yükləyin.";
				notificationEng += "Upload your education certificate.";
				break;
			case "12":
				notificationAze += " Sertifikasiya məzuniyyəti üçün əsas hesab edilən sənədi yükləyin.";
				notificationEng += "Upload the document that is considered the basis for the Certification leave.";
				break;
			case "9":
				notificationAze += " Hamiləlik və doğuşa görə xəstəlik vərəqəsinin surətini əlavə edin";
				notificationEng += " Upload a copy of the sick leave certificate for pregnancy and childbirth.";
				break;
			case "5":
				notificationAze += " Övladınızın doğum haqqında şəhadətnaməsinin surətini əlavə edin";
				notificationEng += " Upload the copy of your child's birth certificate.";
				break;
			case "13":
				notificationAze += "Yaradıcılıq məzuniyyəti üçün əsas hesab edlən sənədi əlavə edin";
				notificationEng += "Download the document considered essential for creative leave";
				break;
			}
			string notification = notificationAze + notificationEng;
			context.Notification = new HtmlString (string.Format ("<p style='color:red; font-size:1em;'> {0} </p>", notification));
			form.For (x => x.Notification).Visible (!string.IsNullOrWhiteSpace (notification)).Required (false).ReadOnly (true);
			form.For (x => x.VacationStatementVersion).Visible (context.Initiator != context.SelectedUserEmployee.SystemUser).Required (false).ReadOnly (true);
		}

		public void GetHolidayDatesFromObject (Context context)
		{
			context.ExceptionalDays.Clear ();
			if (context.StatementDate.HasValue && context.VacationTimeSpan.HasValue) {
				string eql = "";
				context.VacationEndDate = (context.StatementDate.Value + context.VacationTimeSpan.Value).AddDays (-1);
				//				parameters.ErrorMessage = parameters.EndDate.Value.ToString("dd.MM.yyyy") + " first EndDate\n";
				string vacationStartDay = context.StatementDate.Value.Day.ToString ();
				string vacationStartMonth = context.StatementDate.Value.Month.ToString ();
				string vacationStartYear = context.StatementDate.Value.Year.ToString ();
				string vacationStartDate = string.Format ("DateTime({0}, {1}, {2})", vacationStartYear, vacationStartMonth, vacationStartDay);
				string vacationEndDay = context.VacationEndDate.Value.Day.ToString ();
				string vacationEndMonth = context.VacationEndDate.Value.Month.ToString ();
				string vacationEndYear = context.VacationEndDate.Value.Year.ToString ();
				string vacationEndDate = string.Format ("DateTime({0}, {1}, {2})", vacationEndYear, vacationEndMonth, vacationEndDay);
				string firstEql = string.Format ("(StartDate <= {0} AND EndDate >= {0})", vacationStartDate);
				string secondEql = string.Format ("(StartDate <= {0} AND EndDate >= {0})", vacationEndDate);
				string thirdEql = string.Format ("(StartDate >= {0} AND EndDate <= {1})", vacationStartDate, vacationEndDate);
				eql = string.Format ("{0} OR {1} OR {2}", firstEql, secondEql, thirdEql);
				//				parameters.ErrorMessage += eql + "\n";
				context.ExceptionalDays.AddAll (EntityManager<KDBF_ExceptionalDays>.Instance.Find (eql));
				int days = 0;
				List<string> holidayDates = new List<string> ();
				foreach (var holiday in context.ExceptionalDays) {
					if (!holiday.Type.IsProlongation) {
						continue;
					}
					if (holiday.StartDate.Value.Date <= context.StatementDate.Value.Date) {
						//						parameters.ErrorMessage += "if(holiday.StartDate.Value.Date <= parameters.StartDate.Value.Date)\n";
						var daysInterval = (holiday.EndDate.Value.Date - context.StatementDate.Value.Date).Days + 1;
						days += daysInterval;
						for (int i = 0; i < daysInterval; i++) {
							holidayDates.Add (context.StatementDate.Value.AddDays (i).ToString ("dd.MM.yyyy"));
						}
					}
					else
						if (holiday.EndDate.Value.Date >= context.VacationEndDate.Value.Date) {
							//						parameters.ErrorMessage += "if(holiday.EndDate.Value.Date >= parameters.EndDate.Value.Date)\n";
							var daysInterval = (context.VacationEndDate.Value.Date - holiday.StartDate.Value.Date).Days + 1;
							days += daysInterval;
							for (int i = 0; i < daysInterval; i++) {
								holidayDates.Add (holiday.StartDate.Value.AddDays (i).ToString ("dd.MM.yyyy"));
							}
						}
						else
							if (holiday.StartDate.Value.Date >= context.StatementDate.Value.Date && holiday.EndDate.Value.Date <= context.VacationEndDate.Value.Date) {
								//						parameters.ErrorMessage += "if (holiday.StartDate.Value.Date >= parameters.StartDate.Value.Date && holiday.EndDate.Value.Date <= parameters.EndDate.Value.Date)\n";
								var daysInterval = (holiday.EndDate.Value.Date - holiday.StartDate.Value.Date).Days + 1;
								days += daysInterval;
								for (int i = 0; i < daysInterval; i++) {
									holidayDates.Add (holiday.StartDate.Value.AddDays (i).ToString ("dd.MM.yyyy"));
								}
							}
				}
				//				parameters.ErrorMessage += days + " days\n";
				double vacationDays = days + context.VacationTimeSpan.Value.Days;
				//				parameters.ErrorMessage += vacationDays + " vacationDays\n";
				context.StartWork = context.StatementDate.Value.AddDays (vacationDays);
				context.VacationEndDate = context.StartWork.Value.AddDays (-1);
				//				parameters.ErrorMessage += "1-st CheckWeekends\n";
				CheckWeekends (context);
				//				parameters.ErrorMessage += parameters.NextEndDateWorkDays.Value.ToString("dd.MM.yyyy") + " NextEndDateWorkDays\n";
				string nextEndDateWorkDays = string.Format ("DateTime({0}, {1}, {2})", context.StartWork.Value.Year, context.StartWork.Value.Month, context.StartWork.Value.Day);
				eql = string.Format ("StartDate <= {0} AND EndDate >= {0}", nextEndDateWorkDays);
				var hasHolidays = EntityManager<KDBF_ExceptionalDays>.Instance.Find (eql).Any ();
				//				parameters.ErrorMessage += hasHolidays + " hasHolidays\n";
				int tempEndDates = 0;
				bool checkWeekendsFlag = false;
				while (hasHolidays) {
					tempEndDates = GetVacationEndDate (context);
					//					parameters.ErrorMessage += "while CheckWeekends\n";
					checkWeekendsFlag = CheckWeekends (context);
					if (tempEndDates == 0 && !checkWeekendsFlag) {
						break;
					}
					nextEndDateWorkDays = string.Format ("DateTime({0}, {1}, {2})", context.StartWork.Value.Year, context.StartWork.Value.Month, context.StartWork.Value.Day);
					eql = string.Format ("StartDate <= {0} AND EndDate >= {0}", nextEndDateWorkDays);
					hasHolidays = EntityManager<KDBF_ExceptionalDays>.Instance.Find (eql).Any ();
				}
				context.HolidayDays = days;
				context.HolidayDates = string.Join (",", holidayDates);
			}
		}

		public int GetVacationEndDate (Context context)
		{
			//			parameters.ErrorMessage += "GetVacationEndDate\n";
			string nextEndDateWorkDays = string.Format ("DateTime({0}, {1}, {2})", context.StartWork.Value.Year, context.StartWork.Value.Month, context.StartWork.Value.Day);
			string eql = string.Format ("StartDate <= {0} AND EndDate >= {0}", nextEndDateWorkDays);
			var endDateHolidays = EntityManager<KDBF_ExceptionalDays>.Instance.Find (eql);
			//			parameters.ErrorMessage += eql + "\n";
			//			parameters.ErrorMessage += endDateHolidays.Count + " endDateHolidays\n";
			context.ExceptionalDays.AddAll (endDateHolidays);
			var endDate = context.StartWork;
			int endDays = 0;
			foreach (var endDateHoliday in endDateHolidays) {
				if (!endDateHoliday.Type.IsProlongation) {
					continue;
				}
				if (endDateHoliday.EndDate.Value.Date == context.StartWork.Value.Date) {
					endDays++;
				}
				else {
					endDays += (endDateHoliday.EndDate.Value - context.StartWork.Value).Days + 1;
				}
			}
			//			parameters.ErrorMessage += parameters.NextEndDateWorkDays.Value.ToString("dd.MM.yyyy") + " NextEndDateWorkDays\n";
			context.StartWork = endDate.Value.AddDays (endDays);
			//			parameters.ErrorMessage += parameters.NextEndDateWorkDays.Value.ToString("dd.MM.yyyy") + " NextEndDateWorkDays\n";
			return endDays;
		}

		public bool CheckWeekends (Context context)
		{
			//			parameters.ErrorMessage += "CheckWeekends\n";
			var prodCal = Locator.GetServiceNotNull<IProductionCalendarService> ();
			var startDate = context.StartWork;
			bool isWorkDay = prodCal.IsWorkDay (startDate.Value);
			while (!isWorkDay) {
				startDate = startDate.Value.AddDays (1);
				isWorkDay = prodCal.IsWorkDay (startDate.Value);
				//				parameters.ErrorMessage += startDate.Value.ToString("dd.MM.yyyy") + " " + isWorkDay + "\n";
			}
			bool returnedValue = context.StartWork != startDate;
			context.StartWork = startDate;
			return returnedValue;
		}

		/// <summary>
		/// StatementDate_OnChange
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void StatementDate_OnChange (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			try {
				if (context.StatementDate == null) {
					context.Days = null;
					context.StartWork = null;
				}
				form.For (c => c.Substitution).Required (false);
				form.For (c => c.SubstituteOfSubstitution).Required (false);
				WarningWeekends (context, form);
				CalculateStartWorkDay (context, form);
				//CheckRequiredReplacement (context, form);
				SubstitutionCheckRequiredReplacement (context, form);
				FindVacationForEmployee (context);
				GetUsedVacDays (context);
				CalculateAvailableVacDays (context);
				form.For (x => x.FullVacInfo).Visible (context.VacationType.Code.Equals ("1")).ReadOnly (true).Required (false);
			}
			catch (Exception ex) {
				context.VacationAlert = new HtmlString (ex.Message);
				form.For (c => c.VacationAlert).ReadOnly (true).Required (false).Visible (true);
			}
		}

		public virtual void GetUsedVacDays (Context context)
		{
			if (context.VacationType.Code == "1" && context.VacationDaysInfo.Any () && context.Days.HasValue) {
				context.UsedVacDays.Clear ();
				var vacationDays = context.VacationDaysInfo.OrderBy (x => x.StartDate);
				int days = (int)context.Days.Value;
				foreach (var vacInfo in vacationDays) {
					if (vacInfo.AvailableDays <= 0) {
						continue;
					}
					var blockItem = InterfaceActivator.Create<P_Vacation_UsedVacDays> ();
					if (vacInfo.AvailableDays < days) {
						blockItem.Period = vacInfo.StartDate.Value.ToString ("dd.MM.yyyy") + "-" + vacInfo.EndDate.Value.ToString ("dd.MM.yyyy");
						blockItem.DaysCount = vacInfo.AvailableDays;
						blockItem.StartPeriod = vacInfo.StartDate.Value;
						blockItem.EndPeriod = vacInfo.EndDate.Value;
						context.UsedVacDays.Add (blockItem);
						days -= (int)vacInfo.AvailableDays;
					}
					else {
						blockItem.Period = vacInfo.StartDate.Value.ToString ("dd.MM.yyyy") + "-" + vacInfo.EndDate.Value.ToString ("dd.MM.yyyy");
						blockItem.DaysCount = days;
						blockItem.StartPeriod = vacInfo.StartDate.Value;
						blockItem.EndPeriod = vacInfo.EndDate.Value;
						context.UsedVacDays.Add (blockItem);
						break;
					}
				}
				context.VacationDaysCount = context.UsedVacDays.Select (x => x.DaysCount).Sum ();
			}
			if (context.SubstitutionUserEmployee == null && context.Substitution == null) {
				context.SubstitutionBool = false;
			}
		}

		/// <summary>
		/// CreateRequiredReplacement
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void CreateRequiredReplacement (Context context)
		{
			var substitutionEmployee = EntityManager<KBDF_HR_BANK_EMPLOYEE>.Instance.Find ("SystemUser = " + context.Substitution.Id).FirstOrDefault ();
			if (context.RequiredReplacement != null && context.Substitution != null) {
				var requiredReplacement = EntityManager<KBDF_RequiredReplacement>.Instance.Create ();
				requiredReplacement.User = context.Substitution;
				var employee = EntityManager<KBDF_HR_BANK_EMPLOYEE>.Instance.Find ("SystemUser = " + context.Substitution.Id).FirstOrDefault ();
				requiredReplacement.EmployeeId = substitutionEmployee != null ? substitutionEmployee.EmployeeID : "Employee not found";
				requiredReplacement.IsActive = true;
				requiredReplacement.IsSubstitution = true;
				requiredReplacement.IsTemporary = true;
				requiredReplacement.ReplacementType = context.RequiredReplacement.ReplacementType;
				requiredReplacement.StartDate = context.StatementDate;
				requiredReplacement.EndDate = context.StartWork;
				requiredReplacement.Structure = context.BranchOrgItem;
				requiredReplacement.Parent = context.RequiredReplacement;
				requiredReplacement.JiraTaskNumber = context.JiraTaskLink;
				requiredReplacement.Order = new EleWise.ELMA.Common.Models.Attachment () {
					File = context.VacationOrderScan
				};
				requiredReplacement.OrderNumber = context.OrderRegNumber;
				requiredReplacement.Save ();
				context.CreatedRequiredReplacements.Add (requiredReplacement);
				// filial mudiri evez eden ucun
				if (context.RequiredReplacement.ReplacementType.Code == "3") {
					if (context.SubActiveRequiredReplacement != null && context.SubstituteOfSubstitution != null) {
						var subofSubRequiredReplacement = EntityManager<KBDF_RequiredReplacement>.Instance.Create ();
						subofSubRequiredReplacement.User = context.SubstituteOfSubstitution;
						var subofSubEmployee = EntityManager<KBDF_HR_BANK_EMPLOYEE>.Instance.Find ("SystemUser = " + context.SubstituteOfSubstitution.Id).FirstOrDefault ();
						subofSubRequiredReplacement.EmployeeId = subofSubEmployee != null ? subofSubEmployee.EmployeeID : "Employee not found";
						subofSubRequiredReplacement.IsActive = true;
						subofSubRequiredReplacement.IsSubstitution = true;
						subofSubRequiredReplacement.IsTemporary = true;
						subofSubRequiredReplacement.ReplacementType = EntityManager<KBDF_RequiredReplacementType>.Instance.Find ("Name = '3-cü xəzinə inzibatçısı'").FirstOrDefault ();
						subofSubRequiredReplacement.StartDate = context.StatementDate;
						subofSubRequiredReplacement.EndDate = context.StartWork;
						subofSubRequiredReplacement.Structure = context.BranchOrgItem;
						subofSubRequiredReplacement.Parent = context.SubActiveRequiredReplacement;
						subofSubRequiredReplacement.JiraTaskNumber = context.JiraTaskLink;
						subofSubRequiredReplacement.Order = new EleWise.ELMA.Common.Models.Attachment () {
							File = context.VacationOrderScan
						};
						subofSubRequiredReplacement.OrderNumber = context.OrderRegNumber;
						subofSubRequiredReplacement.Save ();
						context.CreatedRequiredReplacements.Add (subofSubRequiredReplacement);
					}
				}
			}
		}

		/// <summary>
		/// GetApprovers
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetApprovers (Context context)
		{
			context.ApprovementCount = 1;
			context.Count = 1;
			var date = DateTime.Now;
			IReplacementFilter filter = InterfaceActivator.Create<IReplacementFilter> ();
			filter.Query = string.Format ("TargetUser = {0} AND StartDate <= DateTime{1} AND EndDate >= DateTime{1}", (long)context.SelectedUser.Id, date.Date.ToString ("(yyyy,MM,dd)"));
			var substitute = EntityManager<Replacement>.Instance.Find (filter, FetchOptions.All).FirstOrDefault ();
			if (substitute != null) {
				var orgsubstitute = EntityManager<KBDF_HR_BANK_EMPLOYEE>.Instance.Find ("SystemUser = " + substitute.SourceUser.Id).FirstOrDefault ();
				context.PositionOrgItem = orgsubstitute.TemporaryPosition != null ? orgsubstitute.TemporaryPosition : orgsubstitute.MainPosition;
			}
			var section = EntityManager<KBDF_OrganizationItems>.Instance.Find (string.Format ("OrganizationItemType in (205,203,204,2,1,207) AND HRBCode = '{0}' AND IsActive = TRUE", context.PositionOrgItem.HRBCode)).FirstOrDefault ();
			KBDF_StructureChiefs sc = null;
			if (section != null) {
				sc = EntityManager<KBDF_StructureChiefs>.Instance.Find (string.Format ("OrgItem = {0} ", section.Id)).FirstOrDefault ();
			}
			else {
				sc = EntityManager<KBDF_StructureChiefs>.Instance.Find (string.Format ("OrgItem = {0} ", context.PositionOrgItem.Id)).FirstOrDefault ();
			}
			if (context.PositionOrgItem.OrganizationItemPosition.Name == "Məhsul sahibi") {
				context.StructureChiefs.AddAll (sc.Approvers.Where (a => a.Position.OrganizationItemPosition.Name != "Funksional lider"));
				context.Count = sc.Approvers.Any (a => a.Position.OrganizationItemPosition.Name.Equals ("Funksional lider")) ? 2 : 1;
			}
			else {
				context.StructureChiefs.AddAll (sc.Approvers);
			}
		}

		/// <summary>
		/// Approve
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void Approve (Context context)
		{
			context.IsApprovement = true;
			if (context.ApprovementCount.HasValue) {
				context.ApprovementCount--;
			}
		}

		/// <summary>
		/// CheckScApprovers
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckScApprovers (Context context)
		{
			if (context.Count > context.StructureChiefs.Count || (context.ApprovementCount.HasValue && context.ApprovementCount.Value == 0)) {
				return 0;
			}
			var scApprover = context.StructureChiefs.FirstOrDefault (x => x.Order == context.Count);
			context.Count++;
			if (scApprover == null) {
				return 0;
			}
			if (!scApprover.Approvers.Any () || context.PositionOrgItem == scApprover.Position || scApprover.Approvers.All (user => user.Status == UserStatus.Blocked) || scApprover.Approvers.Contains (context.SelectedUser)) {
				return 1;
			}
			if (scApprover.Position.OrganizationItemType.Weight < context.PositionOrgItem.OrganizationItemType.Weight) {
				return 1;
			}
			if (scApprover.Position.OrganizationItemType.Code == "DEPUTY" && scApprover.Position.OrganizationItemPosition.HRBCode != "4138") {
				var nextApprover = context.StructureChiefs.FirstOrDefault (x => x.Order == context.Count);
				if (nextApprover != null && nextApprover.Approvers.Any ()) {
					return 1;
				}
			}
			context.ScApprovers.AddAll (scApprover.Approvers);
			if (scApprover.Position.OrganizationItemPosition.HRBCode.Equals ("4012")) {
				context.StructureChiefs.Where (x => x.Position.OrganizationItemPosition.HRBCode.Equals ("4018")).ForEach (x =>  {
					if (!x.Approvers.Contains (context.SelectedUser)) {
						context.ScApprovers.AddAll (x.Approvers);
					}
				});
			}
			var users = new List<User> (context.ScApprovers);
			foreach (var user in users) {
				EleWise.ELMA.Documents.Managers.DmsObjectManager.Instance.AddViewPermission (context.VacationStatement, user);
				var curatorObj = EntityManager<KBDF_CuratorList>.Instance.Find (string.Format ("Curator = {0}", user.Id)).FirstOrDefault ();
				if (curatorObj != null && curatorObj.Assistant.Any ()) {
					context.ScApprovers.AddAll (curatorObj.Assistant);
					foreach (var assistant in curatorObj.Assistant) {
						EleWise.ELMA.Documents.Managers.DmsObjectManager.Instance.AddViewPermission (context.VacationStatement, assistant);
					}
				}
			}
			return 2;
		}

		/// <summary>
		/// Reject
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void Reject (Context context)
		{
			context.IsApprovement = false;
		}

		/// <summary>
		/// CheckSubstitutionAuthority
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void CheckSubstitutionAuthority (Context context)
		{
			if (!context.IsStartAuthorityProsess) {
				GetSubstitutionAuthority (context);
			}
		}

		public void GetSubstitutionAuthority (Context context)
		{
			context.MailText = null;
			try {
				if (context.IsInBranch) {
					context.IsStartAuthorityProsess = false;
					context.IsActiveAuthoritySubstitution = false;
					if (IsChiefBranchAndSubstitution (context)) {
						//						if (context.StartWork.Value.Date <= DateTime.Now.Date)
						//						{
						//							context.IsStartAuthorityProsess = false;
						//							return;
						//						}
						//Get Substitution employee
						context.SubstitutionUserEmployee = null;
						context.SubstitutionUserEmployee = EntityManager<KBDF_HR_BANK_EMPLOYEE>.Instance.Find ("SystemUser = " + context.Substitution.Id).FirstOrDefault ();
						//Substitution user in authority document Search by pin
						if (string.IsNullOrWhiteSpace (context.SubstitutionUserEmployee.FinCode)) {
							throw new Exception ("fin boşdur");
						}
						context.AuthorityInfo.Clear ();
						var docs = EntityManager<KBDF_Authority>.Instance.Find (x => x.AuthorityEndDate.HasValue && (x.IsFullName.HasValue && x.IsFullName.Value) && (x.AuthorityEndDate.Value > context.VacationEndDate.Value && x.AuthorizedOfDate <= context.StatementDate.Value) && (x.AuthorityType2.Id == 1 || x.AuthorityType2.Id == 2)).Where (x => x.PerInfo.Any (v => v.FinCode.Trim ().ToUpper () == context.SubstitutionUserEmployee.FinCode.Trim ().ToUpper ()));
						if (docs.Any ()) {
							foreach (var doc in docs) {
								foreach (var per in doc.PerInfo) {
									if (per.FinCode.Trim ().ToUpper () == context.SubstitutionUserEmployee.FinCode.Trim ().ToUpper ()) {
										var blockitem = InterfaceActivator.Create<P_Vacation_AuthorityInfo> ();
										blockitem.AuthorityUser = per.AuthorizedPersonFullName;
										blockitem.AuthorizedOfDate = doc.AuthorizedOfDate.Value;
										blockitem.AuthorizedEndDate = doc.AuthorityEndDate.Value;
										blockitem.FinCode = per.FinCode;
										//blockitem.BranchOrSection = per.BranchOrSection;
										blockitem.Authorities = doc.Authorities;
										blockitem.DocId = doc.Id;
										context.AuthorityInfo.Add (blockitem);
									}
								}
								//									}
								//								}
							}
							context.FinCode = context.SubstitutionUserEmployee.FinCode.Trim ().ToUpper ();
							context.IsActiveAuthoritySubstitution = true;
							context.IsStartAuthorityProsess = false;
						}
						else {
							context.IsStartAuthorityProsess = true;
							context.FinCode = context.SubstitutionUserEmployee.FinCode.Trim ().ToUpper ();
							var serviceuser = PublicAPI.Portal.Security.User.Find ("UserName = 'ServiceUser'").FirstOrDefault ();
							if (serviceuser == null) {
								throw new Exception ("service user boşdur");
							}
							context.ServiceUser = serviceuser;
						}
					}
				}
			}
			catch (Exception ex) {
			}
		}

		public bool IsChiefBranchAndSubstitution (Context context)
		{
			bool isChiefBranch = context.SelectedUserEmployee.MainPosition.OrganizationItemPosition.HRBCode == "4065" || (context.SelectedUserEmployee.TemporaryPosition != null && context.SelectedUserEmployee.TemporaryPosition.OrganizationItemPosition.HRBCode == "4065");
			if (isChiefBranch && (context.SubstitutionBool.HasValue && context.SubstitutionBool.Value) && context.Substitution != null) {
				return true;
			}
			return false;
		}

		/// <summary>
		/// OnChangeSubstitutionCheckAutority
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void OnChangeSubstitutionCheckAutority (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			context.AuthorityInfo.Clear ();
			if (context.SubstitutionBool.HasValue && context.SubstitutionBool.Value) {
				GetSubstitutionAuthority (context);
			}
			else {
				context.IsStartAuthorityProsess = false;
			}
			form.For (c => c.AuthorityInfo).Visible (context.IsActiveAuthoritySubstitution);
		}

		/// <summary>
		/// GetSettingsForUsers
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetSettingsForUsers (Context context)
		{
		}

		public void FindVacationDaysFromObject (Context context)
		{
			context.VacationDaysInfo.Clear ();
			var vacStartDate = string.Format ("DateTime({0}, {1}, {2})", context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day);
			var vacRecords = EntityManager<KBDF_VacationDays>.Instance.Find (string.Format ("Employee = {0} AND StartDate <= {1} ", context.SelectedUserEmployee.Id, vacStartDate)).OrderBy (x => x.StartDate).ToList ();
			context.VacationDaysInfo.AddAll (vacRecords);
		}

		public void FindVacationForEmployee (Context context)
		{
			if (context.SelectedUserEmployee == null || context.StatementDate == null) {
				return;
			}
			FindVacationDaysFromObject (context);
			context.FullVacInfo.Clear ();
			if (context.StatementDate.HasValue) {
				var vacRecords = context.VacationDaysInfo.Where (x => x.AvailableDays > 0).ToList ();
				foreach (var record in vacRecords) {
					var vacInfo = new P_Vacation_FullVacInfo ();
					vacInfo.StartPeriod = record.StartDate;
					vacInfo.EndPeriod = record.EndDate;
					vacInfo.TotalVacation = record.Days;
					vacInfo.Used = record.UsedDays;
					vacInfo.Unused = record.AvailableDays;
					context.FullVacInfo.Add (vacInfo);
				}
				context.VacationTotalCount = context.FullVacInfo.Any () ? context.FullVacInfo.Select (x => x.Unused).Sum ().Value : 0;
			}
		}

		/// <summary>
		/// StartLoad
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void StartLoad (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			form.For (x => x.MaternityLeave).Visible (false).Required (false).ReadOnly (false);
		}

		public static void CalculateAvailableVacDays (Context context)
		{
			if (context.VacationType != null && context.VacationType.Code.Equals ("1")) {
				if (context.FullVacInfo.Any () && context.StatementDate.HasValue) {
					var currentVacPeriod = context.FullVacInfo.Where (x => x.StartPeriod <= context.StatementDate.Value && x.EndPeriod >= context.StatementDate.Value).FirstOrDefault ();
					if (currentVacPeriod != null) {
						double daysInYear = DateTime.IsLeapYear (DateTime.Now.Year) ? 366 : 365;
						var availableDaysCount = Convert.ToInt32 (Math.Round ((context.StatementDate.Value - currentVacPeriod.StartPeriod.Value).Days * (currentVacPeriod.TotalVacation.Value / daysInYear)));
						context.CurrentAvailableVacDays = context.FullVacInfo.Where (x => x.StartPeriod < currentVacPeriod.StartPeriod).Select (x => x.Unused).ToList ().Sum () + availableDaysCount - currentVacPeriod.Used.Value;
					}
				}
			}
		}

		public static void CalculateMustUseVacDaysCount (Context context)
		{
			if (context.VacationType != null && context.VacationType.Code.Equals ("1")) {
				double daysInYear = DateTime.IsLeapYear (DateTime.Now.Year) ? 366 : 365;
				var lastDateOfYear = new DateTime (DateTime.Now.Year, 12, 31);
				var currentVacPeriod = context.FullVacInfo.Where (x => x.StartPeriod <= DateTime.Now && x.EndPeriod >= DateTime.Now).FirstOrDefault ();
				if (currentVacPeriod != null) {
					var daysCount = Convert.ToInt64 (Math.Round (((lastDateOfYear - currentVacPeriod.StartPeriod.Value).Days / daysInYear) * 34));
					context.MustUsedVacDaysTillEndOfYear = context.FullVacInfo.Where (x => x.StartPeriod < currentVacPeriod.StartPeriod && x.Unused != 0).Select (x => x.Unused).Sum () + daysCount;
				}
			}
		}

		/// <summary>
		/// ReturnVacationDays
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void ReturnVacationDays (ISet<KBDF_VacationDays> vacationDays, long countReturnableDays)
		{
			if (vacationDays.Any ()) {
				var orderedVacations = vacationDays.OrderByDescending (x => x.StartDate);
				var vacationDaysCount = countReturnableDays;
				foreach (var data in orderedVacations) {
					var returnableDaysCount = data.Days - data.AvailableDays;
					if (returnableDaysCount >= vacationDaysCount) {
						data.AvailableDays += vacationDaysCount;
						data.UsedDays -= vacationDaysCount;
						data.Save ();
						break;
					}
					else {
						vacationDaysCount -= returnableDaysCount;
						data.UsedDays -= returnableDaysCount;
						data.AvailableDays += returnableDaysCount;
						data.Save ();
						continue;
					}
					//if (data.UsedDays >= vacationDaysCount) {
					//						data.AvailableDays += vacationDaysCount;
					//						data.UsedDays -= vacationDaysCount;
					//						data.Save ();
					//						break;
					//					}
					//					else {
					//						vacationDaysCount -= data.UsedDays;
					//						data.AvailableDays += data.UsedDays;
					//						data.UsedDays -= data.UsedDays;
					//						data.Save ();
					//						continue;
					//					}
				}
			}
		}

		public class Balance
		{
			public string employeeCode {
				get;
				set;
			}

			public string mainVacationDays {
				get;
				set;
			}

			public string additionalVacationForDegree {
				get;
				set;
			}

			public string additionalVacationForChild {
				get;
				set;
			}

			public string additionalVacationForHarsh {
				get;
				set;
			}

			public string totalVacationDays {
				get;
				set;
			}

			public string usedMainVacationDays {
				get;
				set;
			}

			public string usedAdditionalVacationDays {
				get;
				set;
			}

			public string usedTotalVacationDays {
				get;
				set;
			}

			public string unusedMainVacationDays {
				get;
				set;
			}

			public string unusedAdditionalVacationDays {
				get;
				set;
			}

			public string unusedTotalVacationDays {
				get;
				set;
			}

			public string startVacationPeriod {
				get;
				set;
			}

			public string endVacationPeriod {
				get;
				set;
			}

			public string renewalDate {
				get;
				set;
			}
		}

		public class ResultType
		{
			public bool exists {
				get;
				set;
			}

			public string code {
				get;
				set;
			}

			public string message {
				get;
				set;
			}
		}

		public class Result
		{
			public string msgId {
				get;
				set;
			}

			public ResultType success {
				get;
				set;
			}

			public ResultType warning {
				get;
				set;
			}

			public ResultType error {
				get;
				set;
			}
		}

		public class Response
		{
			public Result result {
				get;
				set;
			}

			public List<Balance> balances {
				get;
				set;
			}
		}

		/// <summary>
		/// ShouldBeOnHrApprove
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object ShouldBeOnHrApprove (Context context)
		{
			if (context.Initiator != context.SelectedUser || context.SupportingDocuments.Any ()) {
				return true;
			}
			return false;
		}

		/// <summary>
		/// SetEimzaApprovers
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void SetEimzaApprovers (Context context)
		{
			context.IsElektronImza = true;
			var eimzaApprover = EntityManager<KBDF_EImzaApprover>.Instance.Find ("IsActive = TRUE").FirstOrDefault ();
			context.ApproversEImza.Add (eimzaApprover.Approver);
		}

		/// <summary>
		/// FindStructureAndVaInfoByEmp
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void FindStructureAndVaInfoByEmp (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			context.DepartmentOrgItem = null;
			context.SectionOrgItem = null;
			context.SubSectionOrgItem = null;
			context.PositionOrgItem = null;
			context.Days = 0;
			context.SelectedUser = null;
			context.VacationType = null;
			context.StatementDate = null;
			context.VacationEndDate = null;
			context.Days = 0;
			context.MustUsedVacDaysTillEndOfYear = 0;
			context.CurrentAvailableVacDays = 0;
			context.VacationAlert = null;
			context.VacationTotalCount = 0;
			//context.PaymentStatus = null;
			context.VacationStatementVersion = null;
			bool isFound = false;
			if (context.SelectedUserEmployee != null) {
				context.SelectedUser = context.SelectedUserEmployee.SystemUser;
				context.PositionOrgItem = (context.SelectedUserEmployee.TemporaryPosition != null) ? context.SelectedUserEmployee.TemporaryPosition : context.SelectedUserEmployee.MainPosition;
				context.SectionOrgItem = (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, "SECTION", out isFound);
				// musteqil shobe axtarishi
				context.DepartmentOrgItem = (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, "INDEPENDENTSECTION", out isFound);
				if (context.DepartmentOrgItem != null && context.DepartmentOrgItem.HRBCode == "1100000000") {
					context.DepartmentOrgItem = null;
				}
				// musteqil shobe varsa bolme axtarisi
				if (context.DepartmentOrgItem != null) {
					context.SubSectionOrgItem = context.PositionOrgItem != null ? (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, "SUBSECTION", out isFound) : null;
				}
				// musteqil shobe yoxdusa traditional shobe bolme department filial axtarisi
				if (context.DepartmentOrgItem == null) {
					// shobe axtarishi
					context.SectionOrgItem = context.PositionOrgItem != null ? (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, "SECTION", out isFound) : null;
					context.SubSectionOrgItem = context.PositionOrgItem != null ? (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, "SUBSECTION", out isFound) : null;
					context.DepartmentOrgItem = context.PositionOrgItem != null ? (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, context.PositionOrgItem.IsInBranch ? "BRANCH" : "DEPARTMENT", out isFound) : null;
				}
				if (context.SectionOrgItem != null && context.SectionOrgItem.HRBCode == "1100000000") {
					context.SectionOrgItem = null;
				}
				if (context.SubSectionOrgItem != null && context.SubSectionOrgItem.HRBCode == "1100000000") {
					context.SubSectionOrgItem = null;
				}
				if (context.DepartmentOrgItem != null && context.DepartmentOrgItem.HRBCode == "1100000000") {
					context.DepartmentOrgItem = null;
				}
				if (CheckSubstitutionRequired (context)) {
					form.For (x => x.SubstitutionUserEmployee).Visible (true).Required (false).ReadOnly (false);
				}
				else {
					form.For (x => x.SubstituteOfSubstitutionEmp).Visible (false).Required (false).ReadOnly (false);
					form.For (x => x.SubstitutionUserEmployee).Visible (false).Required (false).ReadOnly (false);
				}
				FindVacationForEmployee (context);
				SubstitutionCheckRequiredReplacement (context, form);
				CalculateMustUseVacDaysCount (context);
				form.For (x => x.SupportingDocuments).Visible (context.Initiator != context.SelectedUserEmployee.SystemUser).Required (context.Initiator != context.SelectedUserEmployee.SystemUser);
				form.For (x => x.SupportingDocuments).Visible (context.SectionOrgItem != null).Required (false).ReadOnly (true);
				form.For (x => x.SupportingDocuments).Visible (context.SubSectionOrgItem != null).Required (false).ReadOnly (true);
				context.OrganizationItemPosition = context.PositionOrgItem.OrganizationItemPosition;
			}
		}

		/// <summary>
		/// SetSigningToFalse
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void SetSigningToFalse (Context context)
		{
			// if vacation was not signed 
			context.OnlyOnePosition = false;
		}

		/// <summary>
		/// ExecuteAutoSigning
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void ExecuteAutoSigning (Context context)
		{
			context.JiraTeamSuccess = false;
			var task = DocumentManager.Instance.GetTasksByDocument (context.VacationOrder, TaskBaseExtensions.ActiveTaskStatuses.ToList (), null).FirstOrDefault ();
			//step 1. Push the process
			var tasksFilter = InterfaceActivator.Create<IWorkflowTaskBaseFilter> ();
			//			if (task == null)
			//			{
			//				task = DocumentManager.Instance.GetTasksByDocument(null, TaskBaseExtensions.CloseStatuses.ToList(), null).FirstOrDefault(a => ((IWorkflowTaskBase)a).WorkflowBookmark.ElementUid == Guid.Parse("fb723a8f-e736-4d5e-8e89-fc460b15841e"));
			//				if (task == null)
			//				{
			//					context.JiraTeamSuccess = false;
			//					return;
			//				}
			//			}
			//			if (TaskBaseExtensions.ActiveTaskStatuses.ToList().Contains(task.Status))
			//			{
			//				
			//				//надо исполнять задачу
			//			}
			//			if(context.WorkflowInstance.Id == 31883442)
			//			{
			//				tasksFilter.Id = 33596554;
			//				context.JiraTeamSuccess = true;
			//			}
			//			if(context.WorkflowInstance.Id == 31898247)
			//			{
			//				tasksFilter.Id = 33596917;
			//				context.JiraTeamSuccess = true;
			//			}
			//			
			//			if(context.WorkflowInstance.Id == 31889928)
			//			{
			//				tasksFilter.Id = 33595103;
			//				context.JiraTeamSuccess = true;
			//			}
			//			
			//			if(context.WorkflowInstance.Id == 31869478)
			//			{
			//				tasksFilter.Id = 33590389;
			//				context.JiraTeamSuccess = true;
			//			}
			//			if(context.WorkflowInstance.Id == 31878547)
			//			{
			//				tasksFilter.Id = 33590909;
			//				context.JiraTeamSuccess = true;
			//			}
			//			if(context.WorkflowInstance.Id == 31868639)
			//			{
			//				tasksFilter.Id = 33590700;
			//				context.JiraTeamSuccess = true;
			//			}
			//			if(context.WorkflowInstance.Id == 31870586)
			//			{
			//				tasksFilter.Id = 33589768;
			//				context.JiraTeamSuccess = true;
			//			}
			//			if(context.WorkflowInstance.Id == 31878257)
			//			{
			//				tasksFilter.Id = 33590912;
			//				context.JiraTeamSuccess = true;
			//			}
			//			if(context.WorkflowInstance.Id == 31871635)
			//			{
			//				tasksFilter.Id = 33589820;
			//				context.JiraTeamSuccess = true;
			//			}
			//			if(context.WorkflowInstance.Id == 31878382)
			//			{
			//				tasksFilter.Id = 33590907;
			//				context.JiraTeamSuccess = true;
			//			}
			//			if(context.WorkflowInstance.Id == 31886682)
			//			{
			//				tasksFilter.Id = 33596481;
			//				context.JiraTeamSuccess = true;
			//			}
			//
			//			if(context.WorkflowInstance.Id == 31885859)
			//			{
			//				tasksFilter.Id = 33593422;
			//				context.JiraTeamSuccess = true;
			//			}
			//			if (!context.JiraTeamSuccess)
			//			{
			//				return;
			//			}
			if (task != null) {
				tasksFilter.Id = task.Id;
				context.JiraTeamSuccess = true;
			}
			else {
				context.JiraTeamSuccess = false;
				return;
			}
			// Присваиваем идентификатор процесса, для которого нужно получить активные задачи
			tasksFilter.DisableSecurity = true;
			// Отключаем проверку прав доступа к задачам
			tasksFilter.Statuses = TaskBaseExtensions.ActiveTaskStatuses.ToList ();
			// Выбираем только активные задачи
			var taskBase = WorkflowTaskBaseManager.Instance.Find (tasksFilter, null).FirstOrDefault ();
			if (taskBase != null) {
				var el = taskBase.WorkflowBookmark.Instance.Process.Diagram.Elements.Single (e => e.Uid == taskBase.WorkflowBookmark.ElementUid);
				var connector = el.OutputConnectors.FirstOrDefault (c => c.Name.Trim () == "Təsdiq et");
				if (connector != null) {
					var info = InterfaceActivator.Create<WorkflowTaskInfo> ();
					var service = Locator.GetService<IWorkflowRuntimeService> ();
					service.Execute (new WorkflowTaskExecuteData (taskBase, connector.Uid));
				}
			}
		}

		public class DocInfo
		{
			public long? rezItem;

			public long? itemParentList;
		}

		public DocInfo GetData (long docid, long userid)
		{
			var trProvider = Locator.GetServiceNotNull<ITransformationProvider> ();
			var pattern = @"
							select top 1 rez.Item 'rezItem' , item.ParentList 'itemParentList'
							from KBDF_VacationOrder doc
							join ApprovementResult rez on rez.Document = doc.Id
							join ApprovementListItem item on item.Id = rez.Item
							where doc.Id = {0} and item.[User] = {1}";
			var result = new DocInfo ();
			//context.Log = "";
			//AddTimeMarker("0. sql_query: " + string.Format(pattern, docid, userid));
			using (var r = trProvider.ExecuteQuery (string.Format (pattern, docid, userid))) {
				while (r.Read ()) {
					result = new DocInfo () {
						rezItem = Convert.ToInt64 (r [0]),
						itemParentList = Convert.ToInt64 (r [1]),
					};
				}
			}
			return result;
		}

		public void AddVacationInfo (Context context)
		{
			var vacInfo = EntityManager<KBDF_VacationInfo>.Instance.Create ();
			vacInfo.Name = context.StatementDate.Value.ToString ("dd.MM.yyyy") + " - " + context.VacationEndDate.Value.ToString ("dd.MM.yyyy") + string.Format ("({0})", context.SelectedUserEmployee.FullName);
			vacInfo.CreationAuthor = context.Initiator;
			vacInfo.CreationDate = DateTime.Now;
			vacInfo.Document = context.VacationStatement;
			vacInfo.Employee = context.SelectedUserEmployee;
			vacInfo.VacationType = context.VacationType;
			vacInfo.Status = new DropDownItem ("0", "Rəhbərin təsdiqindədir");
			vacInfo.StartDate = context.StatementDate;
			vacInfo.EndDate = context.VacationEndDate;
			if (context.VacationType.Code.Equals ("1")) {
				vacInfo.UsedDays.AddAll (CreateUsedVacationPeriods (context, context.UsedVacDays));
			}
			else {
				var usedDay = EntityManager<KBDF_UsedDays>.Instance.Create ();
				usedDay.Days = context.Days.Value;
				usedDay.Save ();
				vacInfo.UsedDays.Add (usedDay);
			}
			vacInfo.Save ();
			context.VacationInfo = vacInfo;
		}

		public List<KBDF_UsedDays> CreateUsedVacationPeriods (Context context, ISet<P_Vacation_UsedVacDays> usedVacationPeriods)
		{
			var usedVacDays = new List<KBDF_UsedDays> ();
			foreach (var period in usedVacationPeriods) {
				var usedDay = EntityManager<KBDF_UsedDays>.Instance.Create ();
				usedDay.Name = period.StartPeriod.ToString ("dd.MM.yyyy") + " - " + period.EndPeriod.ToString ("dd.MM.yyyy");
				usedDay.Days = period.DaysCount;
				usedDay.Period = context.VacationDaysInfo.Where (x => x.StartDate.Equals (period.StartPeriod) && x.EndDate.Equals (period.EndPeriod)).FirstOrDefault ();
				usedDay.Save ();
				usedVacDays.Add (usedDay);
			}
			return usedVacDays;
		}

		/// <summary>
		/// DeleteVacationInfo
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void DeleteVacationInfo (Context context)
		{
			foreach (var usedInfo in context.VacationInfo.UsedDays) {
				EntityManager<KBDF_UsedDays>.Instance.Delete (usedInfo);
			}
			var vacationInfo = context.VacationInfo;
			context.VacationInfo = null;
			EntityManager<KBDF_VacationInfo>.Instance.Delete (vacationInfo);
			FindVacationForEmployee (context);
		}

		/// <summary>
		/// SetSignDate
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void SetSignDate (Context context)
		{
			DateTime bakuTime = DateTime.UtcNow.AddHours (4);
			//			DateTime bakuTime = new DateTime(DateTime.UtcNow.AddHours(4).Year, 
			//                                  DateTime.UtcNow.AddHours(4).Month, 
			//                                  DateTime.UtcNow.AddHours(4).Day, 
			//                                  9, 1, 0); 
			TimeZoneInfo bakuTimeZone = TimeZoneInfo.FindSystemTimeZoneById ("Azerbaijan Standard Time");
			if (context.SubstitutionUserEmployee != null) {
				bakuTime = (bakuTime.Hour >= 0 && bakuTime.Hour < 9) ? TimeZoneInfo.ConvertTime (new DateTime (bakuTime.Year, bakuTime.Month, bakuTime.Day, 9, 0, 0), bakuTimeZone) : bakuTime;
			}
			else {
				if (bakuTime.Hour >= 0 && bakuTime.Hour < 10) {
					bakuTime = TimeZoneInfo.ConvertTime (new DateTime (bakuTime.Year, bakuTime.Month, bakuTime.Day, 10, 0, 0), bakuTimeZone);
				}
				else
					if (bakuTime.Hour >= 16 && bakuTime.Hour < 24) {
						bakuTime = TimeZoneInfo.ConvertTime (new DateTime (bakuTime.Year, bakuTime.Month, bakuTime.AddDays (1).Day, 10, 0, 0), bakuTimeZone);
					}
			}
			var calendar = Locator.GetServiceNotNull<IProductionCalendarService> ();
			while (!calendar.IsWorkDay (bakuTime)) {
				bakuTime = bakuTime.AddDays (1);
				bakuTime = new DateTime (bakuTime.Year, bakuTime.Month, bakuTime.Day, context.SubstitutionUserEmployee != null ? 9 : 10, 0, 0);
			}
			context.SignationDate = bakuTime;
		}

		/// <summary>
		/// SetCounterToZero
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void SetCounterToZero (Context context)
		{
			context.Count = 0;
		}

		/// <summary>
		/// CheckJiraAutoRetryCount
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckJiraAutoRetryCount (Context context)
		{
			++context.Count;
			return !context.JiraSuccesss && context.Count < 6;
		}
		
		/// <summary>
		/// DeleteQueryDB
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void DeleteQueryDB(long UserId)
		{
			var provider = Locator.GetServiceNotNull<ITransformationProvider>();
			var query = @"delete VacationUnique where Id = @p1";
			var parameters = new Dictionary<string, object> { { "p1", UserId } };
			provider.ExecuteNonQuery(query, parameters);
		}
		
		/// <summary>
		/// InsertQueryDB
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void InsertQueryDB(long UserId)
		{
			var provider = Locator.GetServiceNotNull<ITransformationProvider>();
			var query = @"INSERT INTO VacationUnique VALUES (@p1)";
			var parameters = new Dictionary<string, object> { { "p1", UserId } };
						
			int maxRetries = 10;
			int retryCount = 0;
			bool success = false;
			
			while (!success && retryCount < maxRetries)
			{
			    try
			    {
			        provider.ExecuteNonQuery(query, parameters);
			        success = true; 
			    }
			    catch (Exception ex)
			    {
			        retryCount++;
					System.Threading.Thread.Sleep(5000); 
			    }
			}
		}
		
		/// <summary>
		/// RecalculateVacationInfo
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void RecalculateVacationInfo (Context context)
		{
			InsertQueryDB(context.Initiator.Id);
			if (!context.VacationType.Code.Equals ("1")) {
				return;
			}
			if (context.VacationInfo != null)
			{
				// Fixed error of false incrementation of availableDays by removing return vacation days function
				// Tahir Tahirli 23.05.2025
//				var eql = string.Format ("Employee = {0} AND VacationType = 1 AND Id <> {1} ", context.SelectedUserEmployee.Id, context.VacationInfo.Id) + " AND Status = '{0}Rəhbərin təsdiqindədir'";
//				var VacationsOnApprove = EntityManager<KBDF_VacationInfo>.Instance.Find (eql).ToList ();
//				long totalDaysOnApprove = 0;
//				if (VacationsOnApprove.Any ()) {
//					var usedVacs = VacationsOnApprove.SelectMany (x => x.UsedDays).ToList ();
//					totalDaysOnApprove = context.Days.Value + usedVacs.Sum (x => x.Days);
//				}
//				totalDaysOnApprove = totalDaysOnApprove == 0 ? context.Days.Value : totalDaysOnApprove;
				
//				ReturnVacationDays (context.VacationDaysInfo, totalDaysOnApprove);
				FindVacationForEmployee (context);
				GetUsedVacDays (context);
				if (context.VacationType.Code.Equals ("1")) {
					DecreaseVacationDays (context.VacationDaysInfo, context.Days.Value);
				}
				context.VacationInfo.UsedDays.ForEach (x =>  {
					x.Info = null;
					x.Info = null;
					EntityManager<KBDF_UsedDays>.Instance.Delete (x);
				});
				context.VacationInfo.UsedDays.Clear ();
				context.VacationInfo.UsedDays.AddAll (CreateUsedVacationPeriods (context, context.UsedVacDays));
			}
			DeleteQueryDB(context.Initiator.Id);
		}

		public void DecreaseVacationDays (ISet<KBDF_VacationDays> vacationDays, long totalDecreasedDays)
		{
			if (vacationDays.Any ()) {
				var orderedVacationPeriods = vacationDays.OrderBy (x => x.StartDate);
				foreach (var vacationPeriod in orderedVacationPeriods) {
					if (vacationPeriod.AvailableDays <= 0) {
						continue;
					}
					if (vacationPeriod.AvailableDays >= totalDecreasedDays) {
						vacationPeriod.AvailableDays -= totalDecreasedDays;
						vacationPeriod.UsedDays += totalDecreasedDays;
						vacationPeriod.Save ();
						break;
					}
					else {
						totalDecreasedDays -= vacationPeriod.AvailableDays;
						vacationPeriod.UsedDays += vacationPeriod.AvailableDays;
						vacationPeriod.AvailableDays -= vacationPeriod.AvailableDays;
						vacationPeriod.Save ();
						continue;
					}
				}
			}
		}

		/// <summary>
		/// CertificateType_OnChange
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		/// <param name="form"></param>
		public virtual void CertificateType_OnChange (Context context, EleWise.ELMA.Model.Views.FormViewBuilder<Context> form)
		{
			if (context.StatementDate != null && context.StartWork != null) {
				GenerateNewStatementVersion (context);
			}
		}

		/// <summary>
		/// GenSignDocNotStandartCase
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GenSignDocNotStandartCase (Context context)
		{
			var docNumber = context.EimzaForRequiredReplacement ? 300031 : 300035;
			BinaryFile file;
			if (DocflowGlobal.DocumentHelper.GenerateDocumentById (docNumber, context, out file)) {
				context.OrderAndApprover = file;
			}
		}

		/// <summary>
		/// ReturnEmployeeVacations
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void ReturnEmployeeVacations (Context context)
		{
			if (context.VacationType.Code == "1") {
				ReturnVacationDays (context.VacationDaysInfo, context.Days.Value);
			}
			FindVacationForEmployee (context);
		}

		/// <summary>
		/// CheckRequiredReplacementForEimza
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object CheckRequiredReplacementForEimza (Context context)
		{
			var requiredReplacement = EntityManager<KBDF_RequiredReplacement>.Instance.Find (string.Format ("EmployeeId = '{0}' AND IsActive = TRUE and StartDate < DateTime({1}, {2}, {3}) and EndDate > DateTime({4}, {5}, {6}) and IsTemporary= True", context.SelectedUserEmployee.EmployeeID, context.StartWork.Value.Year, context.StartWork.Value.Month, context.StartWork.Value.Day, context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day)).FirstOrDefault ();
			requiredReplacement = requiredReplacement == null ? EntityManager<KBDF_RequiredReplacement>.Instance.Find (string.Format ("EmployeeId  = '{0}' and IsActive=TRUE and StartDate <= DateTime({1}, {2}, {3}) and EndDate is NULL", context.SelectedUserEmployee.EmployeeID, context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day)).FirstOrDefault () : requiredReplacement;
			if (requiredReplacement != null) {
				context.EimzaForRequiredReplacement = true;
				return true;
			}
			else
			{
				context.EimzaForRequiredReplacement = false;  // causing error. check process 33321872 (prod). ApprovedDocumentsEImza is null
				return false;
			}
		}

		/// <summary>
		/// NeedTaskJiraTeam
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual object NeedTaskJiraTeam (Context context)
		{
			var hrbCodesJiraTeamTask = new List<string> {
				"4389",
				"4107",
				"4106",
				"4178",
				"4138",
				"4221"
			};
			if (((context.SelectedUserEmployee.TemporaryPosition != null && hrbCodesJiraTeamTask.Contains (context.SelectedUserEmployee.TemporaryPosition.OrganizationItemPosition.HRBCode)) || hrbCodesJiraTeamTask.Contains (context.SelectedUserEmployee.MainPosition.OrganizationItemPosition.HRBCode)) && context.SubstitutionUserEmployee != null) {
				var subPosition = context.SubstitutionUserEmployee.MainPosition.FullName;
				var rotation = EntityManager<KBDF_TemporaryRotation>.Instance.Find ("EmployeeId = '" + context.SubstitutionUserEmployee.EmployeeID + "' AND IsActive = TRUE").FirstOrDefault ();
				if (rotation != null && rotation.TempOrgItem != null) {
					subPosition = rotation.TempOrgItem.FullName;
				}
				context.JiraTaskDescription = "Məzuniyyətə çıxan əməkdaş: " + context.SelectedUserEmployee.EmployeeID + " " + context.SelectedUserEmployee.FullName + " ( " + context.SelectedUser.UserName + " ) " + "\r\n" + "Vəzifə: " + context.PositionOrgItem.FullName + "\r\n" + "Məzuniyyətə çıxma tarixləri: " + context.StatementDate.Value.ToString ("dd.MM.yyyy HH:mm") + " - " + context.VacationEndDate.Value.ToString ("dd.MM.yyyy HH:mm") + "\r\n" + "Işə çıxma tarixi: " + context.StartWork.Value.ToString ("dd.MM.yyyy HH:mm") + "\r\n" + "Əvəz edən şəxs: " + ((context.SubstitutionBool == true && context.SubstitutionUserEmployee != null) ? context.SubstitutionUserEmployee.EmployeeID + " " + context.SubstitutionUserEmployee.FullName + " (" + context.SubstitutionUserEmployee.SystemUser.UserName + ") " + subPosition : "Yoxdur") + "\r\n" + "Əvəz edən şəxsin əvəz edicisi: " + ((context.SubstituteOfSubstitutionEmp != null) ? context.SubstituteOfSubstitutionEmp.EmployeeID + " " + context.SubstituteOfSubstitutionEmp.FullName + " (" + context.SubstituteOfSubstitutionEmp.SystemUser.UserName + ") " : "Yoxdur");
				return true;
			}
			return false;
		}

		/// <summary>
		/// ChangeStatusConfirm
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void ChangeStatusConfirm (Context context)
		{
			context.ApproveEImza = context.ApproversEImza.FirstOrDefault ();
			//AddTimeMarker("1. taskId: " + task.Id + "; Document: " + element.MBD_Letter.Id);
			//step 2. Approvement List fixing
			var result = GetData (context.VacationOrder.Id, context.ApproveEImza.Id);
			var rez = ApprovementResultManager.Instance.Find (string.Format ("Document = {0}", context.VacationOrder.Id.ToString ())).FirstOrDefault ();
			//var rez = EntityManager<ApprovementResult>.Instance.Find(string.Format("Document = {0}", element.MBD_Letter.Id.ToString())).FirstOrDefault();
			rez.Status = ApprovalStatus.Confirm;
			rez.Save ();
			//AddTimeMarker("2. rez: " + rez.Item.ToString());
			var listItem = EntityManager<ApprovementListItem>.Instance.Find (string.Format ("Id = {0}", result.rezItem.ToString ())).FirstOrDefault ();
			listItem.Comment = "AutoApprove";
			listItem.SolutionDate = DateTime.Now;
			listItem.Save ();
			//AddTimeMarker("3. listItem: " + listItem.Id);
			var taskGroup = EntityManager<ApprovementTaskGroup>.Instance.Find (string.Format ("ApprovementList = {0}", result.itemParentList.ToString ())).FirstOrDefault ();
			taskGroup.Status = ApprovalStatus.None;
			taskGroup.Save ();
			context.JiraTeamSuccess = false;
		}

		/// <summary>
		/// GetApproverEimzaPosition
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void GetApproverEimzaPosition (Context context)
		{
			context.ApproveEImza = context.ApproversEImza.FirstOrDefault ();
			context.ApproverEImza = EntityManager<KBDF_OrganizationItems>.Instance.Find (string.Format ("Users = {0}", context.ApproveEImza.Id)).FirstOrDefault ();
		}

		/// <summary>
		/// CheckForOnChanges
		/// </summary>
		/// <param name="context">Контекст процесса</param>
		public virtual void CheckForOnChanges (Context context)
		{
			ExternalLogBuilder(context, "CheckForOnChanges start");
			
			var formProperties = new List<FormProperty>
			{
				new FormProperty
				{
					Name = "SelectedUserEmployee",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.SelectedUserEmployee
				},
                new FormProperty
				{
					Name = "BranchOrgItem",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.BranchOrgItem
				},
                new FormProperty
				{
					Name = "DepartmentOrgItem",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.DepartmentOrgItem
				},
                new FormProperty
				{
					Name = "SectionOrgItem",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.SectionOrgItem
				},
                new FormProperty
				{
					Name = "SubSectionOrgItem",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.SubSectionOrgItem
				},
                new FormProperty
				{
					Name = "OrganizationItemPosition",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.OrganizationItemPosition
				},
                new FormProperty
				{
					Name = "SubstitutionUserEmployee",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.SubstitutionUserEmployee
				},
                new FormProperty
				{
					Name = "SubstituteOfSubstitutionEmp",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.SubstituteOfSubstitutionEmp
				},
                new FormProperty
				{
					Name = "VacationType",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = true,
					Data = context.VacationType
				},
                new FormProperty
				{
					Name = "CertificationField",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.CertificationField
				},
                new FormProperty
				{
					Name = "CertificationType",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.CertificationType
				},
                new FormProperty
				{
					Name = "DogDate",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.DogDate
				},
                new FormProperty
				{
					Name = "MedicalCertificateNumber",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.MedicalCertificateNumber
				},
                new FormProperty
				{
					Name = "StatementDate",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = true,
					Data = context.StatementDate
				},
                new FormProperty
				{
					Name = "VacationEndDate",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.VacationEndDate
				},
                new FormProperty
				{
					Name = "MarriageDays",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.MarriageDays
				},
                new FormProperty
				{
					Name = "VacationTotalCount",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.VacationTotalCount
				},
                new FormProperty
				{
					Name = "Days",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.Days
				},
                new FormProperty
				{
					Name = "PaymentStatus",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.PaymentStatus
				},
                new FormProperty
				{
					Name = "StartWork",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.StartWork
				},
                new FormProperty
				{
					Name = "VacationStatementVersion",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.VacationStatementVersion
				},
                new FormProperty
				{
					Name = "SupportingDocuments",
					ReadOnlyStatus = false,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.SupportingDocuments
				},
                new FormProperty
				{
					Name = "ReplacementWarning",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.ReplacementWarning
				},
                new FormProperty
				{
					Name = "Notification",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.Notification
				},
                new FormProperty
				{
					Name = "VacationAlert",
					ReadOnlyStatus = true,
					VisibleStatus = true,
					RequiredStatus = false,
					Data = context.VacationAlert
				}
			};
			
			var formPropertyService = new FormPropertyService(formProperties);
			
			try
			{
				#region Start Form OnLoad
				StartForm(context, formPropertyService);
				#endregion
				#region Əməkdaş OnChange()
				FindStructureAndVaInfoByEmp(context, formPropertyService);
				#endregion
				#region Əvəz edən şəxs OnChange()
				GetSubstitutionUserInfo(context, formPropertyService);
				#endregion
				#region Əvəz edən şəxsin əvəz edicisi OnChange()
				SearchOrChangeSubstitutionValue(context);
				#endregion
				#region Məzuniyyət növü OnChange()
				VacTypeDynamic(context, formPropertyService);
				#endregion
				#region Sahə OnChange()
				CertificationFieldChange(context, formPropertyService);
				#endregion
				#region Sertifikat OnChange()
				CertificateType_OnChange(context, formPropertyService);
				#endregion
				#region Məzuniyyətə çıxma tarixi OnChange()
				StatementDate_OnChange(context, formPropertyService);
				#endregion
				#region Məzuniyyətin bitmə tarixi OnChange()
				Days_OnChange(context, formPropertyService);
				#endregion
				#region Günlər OnChange()
				Days_OnChange(context, formPropertyService);
				#endregion
			}
			catch(Exception e)
			{
				ExternalLogBuilder(context, "Error Message", e.Message);
				ExternalLogBuilder(context, "Error StackTrace", e.StackTrace);
			}
			
			var errorMessages = formPropertyService.Validate();
			context.SuccessExternal = errorMessages.Count == 0;
			context.ResultExternal = string.Empty;
			
			foreach(var errorMessage in errorMessages)
			{
				context.ResultExternal = string.Format("{0} \n {1} \n", context.ResultExternal, errorMessage);
				//it could be JSON
			}
			foreach(var element in formPropertyService.GetAll())
			{
				string dataStr = element.Data == null ? "null" : element.Data.ToString();
				ExternalLogBuilder(context, "-----");
				ExternalLogBuilder(context, "Name", element.Name);
				ExternalLogBuilder(context, "ReadOnly", element.ReadOnlyStatus.ToString());
				ExternalLogBuilder(context, "Visible", element.VisibleStatus.ToString());
				ExternalLogBuilder(context, "Required", element.RequiredStatus.ToString());
				ExternalLogBuilder(context, "Data", dataStr);
				ExternalLogBuilder(context, "-----");
			}
			
			ExternalLogBuilder(context, "CheckForOnChanges ended");
		}
		
		// OnLoad Script
		public virtual void StartForm(Context context, FormPropertyService formPropertyService)
		{
			ExternalLogBuilder(context, "StartForm started");
			var typeIds = EntityManager<KBDF_HR_VacationType>.Instance.FindAll ().OrderBy (x => x.Id).Select (x => x.Id).ToList ();
			var contactSettings = (EntitySettings)context.GetSettingsFor (c => c.VacationType);
			contactSettings.FilterQuery = string.Format ("Id in ({0})", string.Join (",", typeIds));
			contactSettings.Save ();
			formPropertyService.Get ("VacationAlert").Visible (false).SetData(context.VacationAlert);
			formPropertyService.Get ("Notification").Visible (false).SetData(context.Notification);
			bool isCertification = context.VacationType != null && context.VacationType.Code == "12";
			formPropertyService.Get ("MarriageDays").ReadOnly (true).Required (false).Visible (false).SetData(context.MarriageDays);
			formPropertyService.Get ("VacationEndDate").ReadOnly (true).Required (false).Visible (false).SetData(context.VacationEndDate);
			formPropertyService.Get ("Days").ReadOnly (isCertification).Required (true).Visible (true).SetData(context.Days);
			formPropertyService.Get ("DogDate").ReadOnly (false).Required (false).Visible (false).SetData(context.DogDate);
			formPropertyService.Get ("CertificationField").Visible (isCertification).Required (isCertification).SetData(context.CertificationField);
			formPropertyService.Get ("CertificationType").Visible (isCertification).Required (isCertification).SetData(context.CertificationType);
			formPropertyService.Get ("DogDate").ReadOnly (false).Required (false).Visible (false).SetData(context.DogDate);
			formPropertyService.Add(new FormProperty("DxshahaddetDate", false, false, false, context.DxshahaddetDate));
			//if (context.BranchOrgItem == null) {
			formPropertyService.Get ("BranchOrgItem").ReadOnly (true).Required (false).Visible (false).SetData(context.BranchOrgItem);
			//}
			if (context.DepartmentOrgItem == null) {
				formPropertyService.Get ("DepartmentOrgItem").ReadOnly (true).Required (false).Visible (false).SetData(context.DepartmentOrgItem);
			}
			if (context.SectionOrgItem == null) {
				formPropertyService.Get ("SectionOrgItem").ReadOnly (true).Required (false).Visible (false).SetData(context.SectionOrgItem);
			}
			if (context.PositionOrgItem == null) {
				//form.For (c => c.PositionOrgItem).ReadOnly (true).Required (false).Visible (false);
				formPropertyService.Add(new FormProperty("SubstitutionBool", true, true, true, context.SubstitutionBool));
			}
			formPropertyService.Get ("SupportingDocuments").Required (isCertification || context.SelectedUser != context.Initiator).SetData(context.SupportingDocuments);
			formPropertyService.Get ("SubstitutionUserEmployee").Visible (CheckSubstitutionRequired (context)).Required (false).ReadOnly (false).SetData(context.SubstitutionUserEmployee);
			formPropertyService.Get ("SubstituteOfSubstitutionEmp").Visible (false).Required (false).ReadOnly (false).SetData(context.SubstituteOfSubstitutionEmp);
			formPropertyService.Get ("VacationStatementVersion").Visible (context.VacationType != null && context.VacationType.Code.Equals ("1")).ReadOnly (true).SetData(context.VacationStatementVersion);
			formPropertyService.Get ("PaymentStatus").Visible (context.VacationType != null && context.VacationType.Code.Equals ("1")).Required (context.VacationType != null && context.VacationType.Code.Equals ("1")).ReadOnly (false).SetData(context.PaymentStatus);
			formPropertyService.Add(new FormProperty("FullVacInfo", (context.StatementDate != null && context.Days.HasValue), true, false, context.FullVacInfo));
			formPropertyService.Get ("MedicalCertificateNumber").Visible (context.VacationType != null && context.VacationType.Code.Equals ("9")).ReadOnly (false).SetData(context.MedicalCertificateNumber);
			CalculateMustUseVacDaysCount (context);
			ExternalLogBuilder(context, "StartForm ended");
		}
		
		// OnChange Script
		public virtual void FindStructureAndVaInfoByEmp (Context context, FormPropertyService formPropertyService)
		{
			var ctx = formPropertyService.Get("SelectedUserEmployee");
			if(ctx.VisibleStatus && !ctx.ReadOnlyStatus)
			{
				/*context.DepartmentOrgItem = null;
				context.SectionOrgItem = null;
				context.SubSectionOrgItem = null;
				context.PositionOrgItem = null;
				context.Days = 0;
				context.SelectedUser = null;
				context.VacationType = null;
				context.StatementDate = null;
				context.VacationEndDate = null;
				context.Days = 0;
				context.MustUsedVacDaysTillEndOfYear = 0;
				context.CurrentAvailableVacDays = 0;
				context.VacationAlert = null;
				context.VacationTotalCount = 0;
				//context.PaymentStatus = null;
				context.VacationStatementVersion = null;
				*/
				bool isFound = false;
				if (context.SelectedUserEmployee != null) {
					context.SelectedUser = context.SelectedUserEmployee.SystemUser;
					context.PositionOrgItem = (context.SelectedUserEmployee.TemporaryPosition != null) ? context.SelectedUserEmployee.TemporaryPosition : context.SelectedUserEmployee.MainPosition;
					context.SectionOrgItem = (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, "SECTION", out isFound);
					// musteqil shobe axtarishi
					context.DepartmentOrgItem = (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, "INDEPENDENTSECTION", out isFound);
					if (context.DepartmentOrgItem != null && context.DepartmentOrgItem.HRBCode == "1100000000") {
						context.DepartmentOrgItem = null;
					}
					// musteqil shobe varsa bolme axtarisi
					if (context.DepartmentOrgItem != null) {
						context.SubSectionOrgItem = context.PositionOrgItem != null ? (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, "SUBSECTION", out isFound) : null;
					}
					// musteqil shobe yoxdusa traditional shobe bolme department filial axtarisi
					if (context.DepartmentOrgItem == null) {
						// shobe axtarishi
						context.SectionOrgItem = context.PositionOrgItem != null ? (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, "SECTION", out isFound) : null;
						context.SubSectionOrgItem = context.PositionOrgItem != null ? (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, "SUBSECTION", out isFound) : null;
						context.DepartmentOrgItem = context.PositionOrgItem != null ? (KBDF_OrganizationItems)DocflowHelper.findInHierarchyUpper (context.PositionOrgItem, context.PositionOrgItem.IsInBranch ? "BRANCH" : "DEPARTMENT", out isFound) : null;
					}
					if (context.SectionOrgItem != null && context.SectionOrgItem.HRBCode == "1100000000") {
						context.SectionOrgItem = null;
					}
					if (context.SubSectionOrgItem != null && context.SubSectionOrgItem.HRBCode == "1100000000") {
						context.SubSectionOrgItem = null;
					}
					if (context.DepartmentOrgItem != null && context.DepartmentOrgItem.HRBCode == "1100000000") {
						context.DepartmentOrgItem = null;
					}
					if (CheckSubstitutionRequired (context)) {
						formPropertyService.Get ("SubstitutionUserEmployee").Visible (true).Required (false).ReadOnly (false).SetData(context.SubstitutionUserEmployee);
					}
					else {
						formPropertyService.Get ("SubstituteOfSubstitutionEmp").Visible (false).Required (false).ReadOnly (false).SetData(context.SubstituteOfSubstitutionEmp);
						formPropertyService.Get ("SubstitutionUserEmployee").Visible (false).Required (false).ReadOnly (false).SetData(context.SubstitutionUserEmployee);
					}
					FindVacationForEmployee (context);
					SubstitutionCheckRequiredReplacement (context, formPropertyService);
					CalculateMustUseVacDaysCount (context);
					formPropertyService.Get ("SupportingDocuments").Visible (context.Initiator != context.SelectedUserEmployee.SystemUser).Required (context.Initiator != context.SelectedUserEmployee.SystemUser).SetData(context.SupportingDocuments);
					formPropertyService.Get ("SupportingDocuments").Visible (context.SectionOrgItem != null).Required (false).ReadOnly (true).SetData(context.SupportingDocuments);
					formPropertyService.Get ("SupportingDocuments").Visible (context.SubSectionOrgItem != null).Required (false).ReadOnly (true).SetData(context.SupportingDocuments);
					context.OrganizationItemPosition = context.PositionOrgItem.OrganizationItemPosition;
				}
			}
		}
		
		public virtual void SubstitutionCheckRequiredReplacement (Context context, FormPropertyService formPropertyService)
		{
			/*context.ReplacementWarning = null;
			context.SubActiveRequiredReplacement = null;
			*/
			CheckRequiredReplacement (context, formPropertyService);
			if (context.StatementDate != null && context.StartWork != null && context.SubstitutionUserEmployee != null && context.RequiredReplacement != null) {
				string alertMessage = "";
				// evezedici muveqqeti axtaris inzibatci
				context.SubActiveRequiredReplacement = EntityManager<KBDF_RequiredReplacement>.Instance.Find (string.Format ("User = {0} AND IsActive = TRUE and StartDate < DateTime({1}, {2}, {3}) and EndDate > DateTime({4}, {5}, {6}) and IsTemporary= True", context.SubstitutionUserEmployee.SystemUser.Id, context.StartWork.Value.Year, context.StartWork.Value.Month, context.StartWork.Value.Day, context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day)).FirstOrDefault ();
				if (context.SubActiveRequiredReplacement != null) {
					alertMessage = "<p style='color:red; font-size:1.3em;'>Seçdiyiniz əvəzedici şəxs daimi inzibatçı olduğuna " + "görə əvəzedicinin əvəzedicisi xanası boş olmamalıdır (" + context.SubActiveRequiredReplacement.ReplacementType.Name + " - " + context.SubActiveRequiredReplacement.Structure.Name + " - " + context.SubActiveRequiredReplacement.StartDate.Value.ToString ("dd.MM.yyyy") + "-" + context.SubActiveRequiredReplacement.EndDate.Value.ToString ("dd.MM.yyyy") + ")</p>";
					formPropertyService.Get ("SubstituteOfSubstitutionEmp").Required (true).Visible (true).SetData(context.SubstituteOfSubstitutionEmp);
					context.ReplacementWarning = new HtmlString (alertMessage);
					formPropertyService.Get ("ReplacementWarning").Visible (true).SetData(context.ReplacementWarning);
					return;
				}
				else {
					//evezedici daimi axtaris inzibatci
					context.SubActiveRequiredReplacement = EntityManager<KBDF_RequiredReplacement>.Instance.Find (string.Format ("User = {0} and IsActive=TRUE and StartDate <= DateTime({1}, {2}, {3}) and EndDate is NULL", context.Substitution.Id, context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day)).FirstOrDefault ();
					if (context.SubActiveRequiredReplacement != null) {
						alertMessage = "<p style='color:red; font-size:1.3em;'>Seçdiyiniz əvəzedici şəxs daimi inzibatçı olduğuna " + "görə əvəzedicinin əvəzedicisi xanası boş olmamalıdır (" + context.SubActiveRequiredReplacement.ReplacementType.Name + " - " + context.SubActiveRequiredReplacement.Structure.Name + " - " + context.SubActiveRequiredReplacement.StartDate.Value.ToString ("dd.MM.yyyy") + "-dən" + ")</p>";
						formPropertyService.Get ("SubstituteOfSubstitutionEmp").Required (true).Visible (true).SetData(context.SubstituteOfSubstitutionEmp);
						context.ReplacementWarning = new HtmlString (alertMessage);
						formPropertyService.Get ("ReplacementWarning").Visible (true).SetData(context.ReplacementWarning);
						return;
					}
				}
			}
		}
		
		public virtual void CheckRequiredReplacement (Context context, FormPropertyService formPropertyService)
		{
			/*context.ReplacementWarning = null;
			context.RequiredReplacement = null;
			*/
			if (context.StatementDate != null && context.StartWork != null) {
				string alertMessage = "";
				//muveqqeti axtaris
				context.RequiredReplacement = EntityManager<KBDF_RequiredReplacement>.Instance.Find (string.Format ("User = {0} AND IsActive = TRUE and StartDate < DateTime({1}, {2}, {3}) and EndDate > DateTime({4}, {5}, {6}) and IsTemporary= True", context.SelectedUser.Id, context.StartWork.Value.Year, context.StartWork.Value.Month, context.StartWork.Value.Day, context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day)).FirstOrDefault ();
				if (context.RequiredReplacement != null) {
					alertMessage = "<p style='color:red; font-size:1.3em;'>Seçdiyiniz istifadəçi inzibatçı olduğuna " + "görə əvəzedici şəxs xanası boş olmamalıdır (" + context.RequiredReplacement.ReplacementType.Name + " - " + context.RequiredReplacement.Structure.Name + " - " + context.RequiredReplacement.StartDate.Value.ToString ("dd.MM.yyyy") + "-" + context.RequiredReplacement.EndDate.Value.ToString ("dd.MM.yyyy") + ")</p>";
					context.SubstitutionBool = true;
					formPropertyService.Add(new FormProperty("SubstitutionBool", true, true, true, context.SubstitutionBool));
					formPropertyService.Get ("SubstitutionUserEmployee").Required (true).Visible (true).SetData(context.SubstitutionUserEmployee);
					context.ReplacementWarning = new HtmlString (alertMessage);
					formPropertyService.Get ("ReplacementWarning").Visible (true).SetData(context.ReplacementWarning);
				}
				else {
					//daimi axtaris
					context.RequiredReplacement = EntityManager<KBDF_RequiredReplacement>.Instance.Find (string.Format ("User = {0} and IsActive=TRUE and StartDate <= DateTime({1}, {2}, {3}) and EndDate is NULL ", context.SelectedUser.Id, context.StatementDate.Value.Year, context.StatementDate.Value.Month, context.StatementDate.Value.Day)).FirstOrDefault ();
					if (context.RequiredReplacement != null) {
						alertMessage = "<p style='color:red; font-size:1.3em;'>Seçdiyiniz istifadəçi daimi inzibatçı olduğuna " + "görə əvəzedici şəxs xanası boş olmamalıdır (" + context.RequiredReplacement.ReplacementType.Name + " - " + context.RequiredReplacement.Structure.Name + " - " + context.RequiredReplacement.StartDate.Value.ToString ("dd.MM.yyyy") + "-dən" + ")</p>";
						context.SubstitutionBool = true;
						formPropertyService.Add(new FormProperty("SubstitutionBool", true, true, true, context.SubstitutionBool));
						formPropertyService.Get ("SubstitutionUserEmployee").Required (true).Visible (true).SetData(context.SubstitutionUserEmployee);
						context.ReplacementWarning = new HtmlString (alertMessage);
						formPropertyService.Get ("ReplacementWarning").Visible (true).SetData(context.ReplacementWarning);
					}
				}
			}
		}
		
		//OnChange Script
		public virtual void GetSubstitutionUserInfo (Context context, FormPropertyService formPropertyService)
		{
			var ctx = formPropertyService.Get("SubstitutionUserEmployee");
			if (ctx.VisibleStatus && !ctx.ReadOnlyStatus)
			{
				SearchOrChangeSubstitutionValue(context);
				SubstitutionCheckRequiredReplacement(context, formPropertyService);
			}
		}
		
		public void SearchOrChangeSubstitutionValue (Context context)
		{
			context.Substitution = context.SubstitutionUserEmployee != null ? context.SubstitutionUserEmployee.SystemUser : null;
			context.SubstituteOfSubstitution = context.SubstituteOfSubstitutionEmp != null ? context.SubstituteOfSubstitutionEmp.SystemUser : null;
			context.SubstitutionUserPosition = context.SubstitutionUserEmployee != null ? context.SubstitutionUserEmployee.MainPosition : null;
			context.SubsOfSubstituionPosition = context.SubstituteOfSubstitutionEmp != null ? context.SubstituteOfSubstitutionEmp.MainPosition : null;
		}
		
		//OnChange
		public virtual void VacTypeDynamic (Context context, FormPropertyService formPropertyService)
		{
			var ctx = formPropertyService.Get("VacationType");
			if (ctx.VisibleStatus && !ctx.ReadOnlyStatus)
			{
				/*context.StartWork = null;
				context.VacationEndDate = null;
				context.Days = null;
				context.VacationTotalCount = 0;
				*/
				context.UseProductionCalendar = context.VacationType.Code == "1" ? true : false;
				context.Komment = new List<String> {
					"1",
					"3",
					"8",
					"13"
				}.Contains (context.VacationType.Code) ? "ndən" : "dən";
				try {
					ChangeVacationTypeDynamic (context, formPropertyService);
					CalculateStartWorkDay (context, formPropertyService);
					CheckRequiredReplacement (context, formPropertyService);
					SubstitutionCheckRequiredReplacement (context, formPropertyService);
					FindVacationForEmployee (context);
					GetUsedVacDays (context);
					CalculateMustUseVacDaysCount (context);
				}
				catch (Exception ex) {
					context.VacationAlert = new HtmlString (ex.StackTrace);
					formPropertyService.Get ("VacationAlert").ReadOnly (true).Required (false).Visible (true).SetData(context.VacationAlert);
				}
			}
		}
		
		public void ChangeVacationTypeDynamic (Context context, FormPropertyService formPropertyService)
		{
			if (context.VacationType.Code == "6") {
				context.Days = 2;
				formPropertyService.Get ("Days").ReadOnly (true).Required (false).Visible (true).SetData(context.Days);
			}
			else {
				context.MarriageDays = null;
				formPropertyService.Get ("MarriageDays").ReadOnly (true).Required (false).Visible (false).SetData(context.MarriageDays);
				formPropertyService.Get ("Days").ReadOnly (false).Required (true).Visible (true).SetData(context.Days);
				if (context.VacationType.Code == "5" || context.VacationType.Code == "9") {
					context.IncludeExceptionalDaysAndWeenends = false;
					formPropertyService.Get ("Days").ReadOnly (true).SetData(context.Days);
					formPropertyService.Get ("VacationEndDate").ReadOnly (false).Required (true).Visible (true).SetData(context.VacationEndDate);
				}
				else {
					formPropertyService.Get ("Days").ReadOnly (false).SetData(context.Days);
					context.IncludeExceptionalDaysAndWeenends = true;
					context.VacationEndDate = null;
					formPropertyService.Get ("VacationEndDate").ReadOnly (true).Required (false).Visible (false).SetData(context.VacationEndDate);
				}
			}
			formPropertyService.Get ("DogDate").Visible (context.VacationType.Code == "9").Required (false).ReadOnly (false).SetData(context.DogDate);
			formPropertyService.Get ("MedicalCertificateNumber").Visible (context.VacationType.Code == "9").Required (false).ReadOnly (false).SetData(context.MedicalCertificateNumber);
			if (context.VacationType.Code == "12") {
				formPropertyService.Get ("CertificationField").Visible (true).Required (true).SetData(context.CertificationField);
				context.Days = 2;
				formPropertyService.Get ("Days").ReadOnly (true).SetData(context.Days);
				//GetStartDate (context, form);
			}
			else {
				//form.For (x => x.Days).ReadOnly (false);
				formPropertyService.Get ("CertificationField").Visible (false).Required (false).SetData(context.CertificationField);
				formPropertyService.Get ("CertificationType").Visible (false).Required (false).SetData(context.CertificationType);
				context.CertificationField = null;
				context.CertificationType = null;
			}
			if (context.VacationType.Code == "8") {
				context.Days = 1;
				formPropertyService.Get ("Days").Visible (true).ReadOnly (true).SetData(context.Days);
			}
			if (context.VacationType.Code == "7") {
				context.Days = 3;
				formPropertyService.Get ("Days").Visible (true).ReadOnly (true).SetData(context.Days);
			}
			var settingz = (DateTimeSettings)context.GetSettingsFor (c => c.StatementDate);
			if (context.VacationType != null && context.VacationType.Code == "1") {
				if (!context.HRSection.Contains (context.Initiator)) {
					settingz.MinDateValue = DateTime.Today.AddMonths (-1);
				}
				formPropertyService.Get ("PaymentStatus").Visible (true).Required (true).ReadOnly (false).SetData(context.PaymentStatus);
			}
			else {
				context.UseProductionCalendar = false;
				settingz.MinDateValue = new DateTime ();
				formPropertyService.Get ("PaymentStatus").Visible (false).Required (false).ReadOnly (true).SetData(context.PaymentStatus);
			}
			formPropertyService.Get ("SupportingDocuments").Visible (true).ReadOnly (false).Required (new List<string> {
				"3",
				"5",
				"9",
				"12",
				"13"
			}.Contains (context.VacationType.Code) || context.SelectedUser != context.Initiator).SetData(context.SupportingDocuments);
			settingz.Save ();
			formPropertyService.Get ("VacationTotalCount").Visible (context.VacationType.Code.Equals ("1")).ReadOnly (true).SetData(context.VacationTotalCount);
		}
		
		public void CalculateStartWorkDay (Context context, FormPropertyService formPropertyService)
		{
			context.VacationTimeSpan = null;
			context.StartWork = null;
			if ((context.Days == null || context.StatementDate == null || context.VacationType == null) && context.IncludeExceptionalDaysAndWeenends != false) {
				return;
			}
			if (context.Days.HasValue) {
				if (context.Days <= 0) {
					context.Days = null;
					formPropertyService.Get("Days").SetData(context.Days);
					return;
				}
				context.VacationTimeSpan = new TimeSpan ((int)context.Days, 0, 0, 0);
			}
			if (context.IncludeExceptionalDaysAndWeenends == false && context.VacationEndDate != null) {
				if (context.VacationEndDate < context.StatementDate) {
					context.VacationEndDate = null;
					context.Days = null;
					return;
				}
				TimeSpan remaindate;
				DateTime d1 = context.VacationEndDate.Value;
				DateTime d2 = context.StatementDate.Value;
				remaindate = d1 - d2;
				var DaysTMP = (long)remaindate.Days;
				context.Days = DaysTMP + 1;
				context.VacationTimeSpan = new TimeSpan ((int)context.Days, 0, 0, 0);
				context.VacationEndDate = context.StatementDate.Value.AddDays (-1) + context.VacationTimeSpan.Value;
				context.StartWork = context.VacationEndDate.Value.AddDays (1);
			}
			else
				if (context.UseProductionCalendar) {
					context.VacationEndDate = null;
					GetHolidayDatesFromObject (context);
				}
				else {
					try {
						//parameters.Success = true;
						var prodCal = Locator.GetServiceNotNull<IProductionCalendarService> ();
						if (context.StatementDate.HasValue && context.VacationTimeSpan.HasValue) {
							context.VacationEndDate = context.StatementDate.Value + context.VacationTimeSpan.Value;
							var evalDate = context.UseProductionCalendar ? prodCal.EvalTargetTime (context.StatementDate.Value, context.VacationTimeSpan.Value) : context.VacationEndDate.Value;
							var delta = 1;
							if (evalDate.Ticks > context.VacationEndDate.Value.Ticks) {
								context.StartWork = evalDate;
								delta = -1;
							}
							else {
								//						parameters.PrevEndDateWorkDays = evalDate;
							}
							var isWorkDay = prodCal.IsWorkDay (context.VacationEndDate.Value);
							var workDate = isWorkDay ? evalDate : context.VacationEndDate.Value;
							while (!isWorkDay) {
								workDate = workDate.AddDays (delta);
								isWorkDay = prodCal.IsWorkDay (workDate);
							}
							var wTime = prodCal.GetWorkTimeStart (workDate);
							if (delta == 1) {
								//wTime = prodCal.GetWorkTimeStart(workDate);
								context.StartWork = new DateTime (workDate.Year, workDate.Month, workDate.Day, wTime.Hours, wTime.Minutes, wTime.Seconds);
							}
							context.VacationEndDate = context.VacationEndDate.Value.AddDays (-1);
						}
					}
					catch (Exception ex) {
						//	parameters.ErrorMessage = ex.Message;
						//	parameters.Success = false;
					}
				}
			GenerateNewStatementVersion (context);
			string notificationAze = "";
			string notificationEng = "";
			if (context.Initiator != context.SelectedUserEmployee.SystemUser && context.StartWork.HasValue) {
				notificationAze += "Sizə təqdim olunmuş ərizəni çap etdikdən sonra müvafiq hissədən məzuniyyətə çıxacaq əməkdaşın imzalamasını təmin edin." + "Daha sonra imzalı sənədi  skan edin və təsdiqedici sənəd bölməsinə yükləyin.";
				notificationEng += "After printing the application submitted to you, ensure the signature of the employee who will be on leave from the relevant department." + "Then scan the signed document  and upload it to the confirmation document section.";
			}
			switch (context.VacationType.Code) {
			case "3":
				notificationAze += " Təhsil haqqında arayış sənədini yükləyin.";
				notificationEng += "Upload your education certificate.";
				break;
			case "12":
				notificationAze += " Sertifikasiya məzuniyyəti üçün əsas hesab edilən sənədi yükləyin.";
				notificationEng += "Upload the document that is considered the basis for the Certification leave.";
				break;
			case "9":
				notificationAze += " Hamiləlik və doğuşa görə xəstəlik vərəqəsinin surətini əlavə edin";
				notificationEng += " Upload a copy of the sick leave certificate for pregnancy and childbirth.";
				break;
			case "5":
				notificationAze += " Övladınızın doğum haqqında şəhadətnaməsinin surətini əlavə edin";
				notificationEng += " Upload the copy of your child's birth certificate.";
				break;
			case "13":
				notificationAze += "Yaradıcılıq məzuniyyəti üçün əsas hesab edlən sənədi əlavə edin";
				notificationEng += "Download the document considered essential for creative leave";
				break;
			}
			string notification = notificationAze + notificationEng;
			context.Notification = new HtmlString (string.Format ("<p style='color:red; font-size:1em;'> {0} </p>", notification));
			formPropertyService.Get ("Notification").Visible (!string.IsNullOrWhiteSpace (notification)).Required (false).ReadOnly (true).SetData(context.Notification);
			formPropertyService.Get ("VacationStatementVersion").Visible (context.Initiator != context.SelectedUserEmployee.SystemUser).Required (false).ReadOnly (true).SetData(context.VacationStatementVersion);
		}
		
		//OnChange Script
		public virtual void CertificationFieldChange (Context context, FormPropertyService formPropertyService)
		{
			var ctx = formPropertyService.Get("CertificationField");
			if (ctx.VisibleStatus && !ctx.ReadOnlyStatus)
			{
				formPropertyService.Get ("CertificationType").Visible (true).Required (true).SetData(context.CertificationType);
				var settings = (EntitySettings)context.GetSettingsFor (e => e.CertificationType);
				settings.FilterQuery = "Field = " + context.CertificationField.Id;
				settings.Save ();
				if (context.StatementDate != null && context.StartWork != null) {
					GenerateNewStatementVersion (context);
				}
			}
		}
		
		//OnChange Script
		public virtual void CertificateType_OnChange (Context context, FormPropertyService formPropertyService)
		{
			var ctx = formPropertyService.Get("CertificationType");
			if (ctx.VisibleStatus && !ctx.ReadOnlyStatus)
			{
				if (context.StatementDate != null && context.StartWork != null) {
					GenerateNewStatementVersion (context);
				}
			}
		}
		
		//OnChange script
		public virtual void StatementDate_OnChange (Context context, FormPropertyService formPropertyService)
		{
			try
			{
				var ctx = formPropertyService.Get("StatementDate");
				if (ctx.VisibleStatus && !ctx.ReadOnlyStatus)
				{
					if (context.StatementDate == null) {
						context.Days = null;
						context.StartWork = null;
					}
					formPropertyService.Get ("Substitution").Required (false).SetData(context.Substitution);
					formPropertyService.Add(new FormProperty("SubstituteOfSubstitution", requiredStatus: false, data: context.SubstituteOfSubstitution));
					WarningWeekends (context, formPropertyService);
					CalculateStartWorkDay (context, formPropertyService);
					//CheckRequiredReplacement (context, form);
					SubstitutionCheckRequiredReplacement (context, formPropertyService);
					FindVacationForEmployee (context);
					GetUsedVacDays (context);
					CalculateAvailableVacDays (context);
					formPropertyService.Get ("FullVacInfo").Visible (context.VacationType.Code.Equals ("1")).ReadOnly (true).Required (false).SetData(context.FullVacInfo);
				}
			}
			catch (Exception ex) {
				context.VacationAlert = new HtmlString (ex.Message);
				formPropertyService.Get ("VacationAlert").ReadOnly (true).Required (false).Visible (true).SetData(context.VacationAlert);
			}
		}
		
		public virtual void WarningWeekends (Context context, FormPropertyService formPropertyService)
		{
			formPropertyService.Get ("VacationAlert").Visible (false).SetData(context.VacationAlert);
			if (context.VacationType != null && context.VacationType.Code == "12") {
				context.Days = 2;
				formPropertyService.Get ("Days").ReadOnly (true).SetData(context.Days);
			}
			if (context.VacationType != null && (context.VacationType.Code == "5" || context.VacationType.Code == "9")) {
				return;
			}
			var ExceptionDays = EntityManager<KDBF_ExceptionalDays>.Instance.FindAll ();
			var statementDateMonth = context.StatementDate.Value.Month;
			var statementDateYear = context.StatementDate.Value.Year;
			foreach (var day in ExceptionDays) {
				if (day.StartDate.Value.Month == statementDateMonth && day.StartDate.Value.Year == statementDateYear) {
					TimeSpan startDiff = new DateTime (day.StartDate.Value.Year, day.StartDate.Value.Month, day.StartDate.Value.Day) - context.StatementDate.Value;
					TimeSpan endDiff = new DateTime (day.EndDate.Value.Year, day.EndDate.Value.Month, day.EndDate.Value.Day) - context.StatementDate.Value;
					if (startDiff.Days <= 0 && endDiff.Days >= 0) {
						string alertMessage = "<p style='color:red; font-size:1.5em;'>Qeyri iş günlərində məzuniyyət yaradıla bilməz. / Leave cannot be created on non-working days.</p>";
						context.VacationAlert = new HtmlString (alertMessage);
						formPropertyService.Get ("VacationAlert").ReadOnly (true).Required (false).Visible (true).SetData(context.VacationAlert);
						context.StartWork = null;
						context.StatementDate = null;
						return;
					}
				}
			}
			if (context.StatementDate != null) {
				var prodCal = Locator.GetServiceNotNull<IProductionCalendarService> ();
				bool isWorkDay = prodCal.IsWorkDay (context.StatementDate.Value);
				if (!isWorkDay) {
					string alertMessage = "<p style='color:red; font-size:1.5em;'>Qeyri iş günlərində məzuniyyət yaradıla bilməz. / Leave cannot be created on non-working days.</p>";
					context.VacationAlert = new HtmlString (alertMessage);
					formPropertyService.Get ("VacationAlert").ReadOnly (true).Required (false).Visible (true).SetData(context.VacationAlert);
					context.StartWork = null;
					context.StatementDate = null;
					return;
				}
			}
			/*if (context.StatementDate != null && ((int)context.StatementDate.Value.DayOfWeek == 6 || (int)context.StatementDate.Value.DayOfWeek == 0))
				{
					context.StatementDate = null;
					string alertMessage = "<p style='color:red; font-size:2em;'>Qeyri iş günləri məzuniyyət yaratmaq olmaz!</p>";
					context.VacationAlert = new HtmlString(alertMessage);
					form.For(c => c.VacationAlert).ReadOnly(true).Required(false).Visible(true);
					
					
				}*/
		}
		
		//OnChange Script
		public virtual void Days_OnChange (Context context, FormPropertyService formPropertyService)
		{
			var ctx = formPropertyService.Get("VacationEndDate");
			var ctxDays = formPropertyService.Get("Days");
			if ((ctx.VisibleStatus && !ctx.ReadOnlyStatus) || (ctxDays.VisibleStatus && !ctxDays.ReadOnlyStatus))
			{
				formPropertyService.Add(new FormProperty("Substitution", requiredStatus: false, data: context.Substitution));
				formPropertyService.Get ("Substitution").Required (false).SetData(context.Substitution);
				formPropertyService.Add(new FormProperty("SubstituteOfSubstitution", requiredStatus: false, data: context.SubstituteOfSubstitution));
				formPropertyService.Get ("SubstituteOfSubstitution").Required (false).SetData(context.SubstituteOfSubstitution);
				FindVacationForEmployee (context);
				GetUsedVacDays (context);
				CalculateStartWorkDay (context, formPropertyService);
				SubstitutionCheckRequiredReplacement (context, formPropertyService);
				CalculateAvailableVacDays (context);
				CheckVacationRule (context, formPropertyService);
			}
		}
		
		public void CheckVacationRule (Context context, FormPropertyService formPropertyService)
		{
			string notificationText = "";
			if (context.VacationType.Code.Equals ("1")) {
				var vacCount = context.FullVacInfo.Where (x => x.StartPeriod <= context.StatementDate).Select (x => x.Unused).Sum () - context.Days;
				if (vacCount < 0) {
					context.Days = 0;
					context.StatementDate = null;
					context.StartWork = null;
					notificationText = "İstifadə etmək istədiyiniz məzuniyyətin gün sayı cari limitinizdən artıqdır!" + Environment.NewLine + "The number of vacation days you want to use exceeds your current limit!";
					context.VacationAlert = new HtmlString (string.Format ("<p style='color:red; font-size:1.3em;'> {0}</p>", notificationText));
					formPropertyService.Get ("VacationAlert").Visible (true).ReadOnly (true).SetData(context.VacationAlert);
				}
				else
					if (context.CurrentAvailableVacDays < 0) {
						notificationText = string.Format ("Sizin cari günə formalaşmış {0} gün məzuniyyətiniz var.{1} gün avans məzuniyyət istifadə edəcəksiniz.", 0, context.Days) + Environment.NewLine + string.Format ("You currently have {0} days of accrued leave. You will use {1} days of advance leave.", 0, context.Days);
						context.VacationAlert = new HtmlString (string.Format ("<p style='color:red; font-size:1.3em;'> {0} </p>", notificationText));
						formPropertyService.Get ("VacationAlert").Visible (true).ReadOnly (true).SetData(context.VacationAlert);
					}
					else
						if (context.Days > context.CurrentAvailableVacDays) {
							notificationText = string.Format ("Sizin cari günə formalaşmış {0} gün məzuniyyətiniz var.{1} gün avans məzuniyyət istifadə edəcəksiniz.", context.CurrentAvailableVacDays, context.Days - context.CurrentAvailableVacDays) + Environment.NewLine + string.Format ("You currently have {0} days of accrued leave. You will use {1} days of advance leave.", context.CurrentAvailableVacDays, context.Days - context.CurrentAvailableVacDays);
							context.VacationAlert = new HtmlString (string.Format ("<p style='color:red; font-size:1.3em;'> {0} </p>", notificationText));
							formPropertyService.Get ("VacationAlert").Visible (true).ReadOnly (true).SetData(context.VacationAlert);
						}
						else {
							context.VacationAlert = null;
							formPropertyService.Get ("VacationAlert").Visible (false).ReadOnly (true).SetData(context.VacationAlert);
						}
			}
		}
		
		public virtual void ExternalLogBuilder(Context context, string logData)
		{
			context.LogExternal = string.Format("{0} \n {1}", context.LogExternal, logData);
		}
		public virtual void ExternalLogBuilder(Context context, string description, string logData)
		{
			context.LogExternal = string.Format("{0} \n {1}: {2}", context.LogExternal, description, logData);
		}
	}
	
	public class FormProperty
	{
		public FormProperty()
		{
			
		}
		public FormProperty(string name, bool visibleStatus = true, bool readOnlyStatus = false, bool requiredStatus = false, object data = null)
		{
			this.Name = name;
			this.VisibleStatus = visibleStatus;
			this.ReadOnlyStatus = readOnlyStatus;
			this.RequiredStatus = requiredStatus;
			this.Data = data;
		}
		
		public string Name { get; set; }
	    public bool VisibleStatus { get; set; }
	    public bool ReadOnlyStatus { get; set; }
	    public bool RequiredStatus { get; set; }
		public object Data { get; set; }
	    
	    public FormProperty Visible(bool data)
	    {
			this.VisibleStatus = data;
			return this;
	    }
	    public FormProperty ReadOnly(bool data)
	    {
			this.ReadOnlyStatus = data;
			return this;
	    }
	    public FormProperty Required(bool data)
	    {
			this.RequiredStatus = data;
			return this;
	    }
	    public FormProperty SetData(object data)
	    {
			this.Data = data;
			return this;
	    }
	}
	
	public class FormPropertyService{
		private List<FormProperty> listFormProperty;
				
	    public FormPropertyService(List<FormProperty> formProperties)
	    {
			this.listFormProperty = formProperties;  	
	    }
	
	    public void Add(FormProperty formProperty)
	    {
	    	if(!listFormProperty.Any(x => x.Name == formProperty.Name))
	    	{
				listFormProperty.Add(formProperty);
	    	}
	    }
	    public void Remove(FormProperty formProperty)
	    {
	    	if(listFormProperty.Any(x => x.Name == formProperty.Name))
	    	{
				listFormProperty.Remove(formProperty);
	    	}
	    }
	    public FormProperty Get(string name)
	    {
			return listFormProperty.FirstOrDefault(x => x.Name == name);
	    }
	    
	    public List<FormProperty> GetAll()
	    {
			return this.listFormProperty;
	    }
	    
	    public List<string> Validate()
	    {
			List<string> errorMessages = new List<string>();
			foreach(var formProperty in listFormProperty)
			{
				if(formProperty.RequiredStatus && formProperty.Data == null)
				{
					errorMessages.Add(string.Format("{0} cannot be null", formProperty.Name));
				}
				else if(formProperty.RequiredStatus && formProperty.Data != null && string.IsNullOrWhiteSpace(formProperty.Data.ToString()))
				{
					errorMessages.Add(string.Format("{0} cannot be empty", formProperty.Name));
				}
			}
			return errorMessages;
	    }
	}
}
