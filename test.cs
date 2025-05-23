		public virtual void RecalculateVacationInfo (Context context)
		{
			if (!context.VacationType.Code.Equals ("1")) {
				return;
			}
			if (context.VacationInfo != null) {
				var eql = string.Format ("Employee = {0} AND VacationType = 1 AND Id <> {1} ", context.SelectedUserEmployee.Id, context.VacationInfo.Id) + " AND Status = '{0}Rəhbərin təsdiqindədir'";
				var VacationsOnApprove = EntityManager<KBDF_VacationInfo>.Instance.Find (eql).ToList ();
				long totalDaysOnApprove = 0;
				if (VacationsOnApprove.Any ()) {
					var usedVacs = VacationsOnApprove.SelectMany (x => x.UsedDays).ToList ();
					totalDaysOnApprove = context.Days.Value + usedVacs.Sum (x => x.Days);
				}
				totalDaysOnApprove = totalDaysOnApprove == 0 ? context.Days.Value : totalDaysOnApprove;
				// fixed vacation days calculation error 
//				ReturnVacationDays (context.VacationDaysInfo, totalDaysOnApprove); can be removed
				FindVacationForEmployee (context);
				GetUsedVacDays (context);
//				if (!context.VacationType.Code.Equals ("1")) {  can be removed
//					DecreaseVacationDays (context.VacationDaysInfo, totalDaysOnApprove);
					DecreaseVacationDays (context.VacationDaysInfo, context.Days.Value);
//              } 

				context.VacationInfo.UsedDays.ForEach (x =>  {
					x.Info = null;
					x.Info = null;
					EntityManager<KBDF_UsedDays>.Instance.Delete (x);
				});
				context.VacationInfo.UsedDays.Clear ();
				context.VacationInfo.UsedDays.AddAll (CreateUsedVacationPeriods (context, context.UsedVacDays));
			}
		}