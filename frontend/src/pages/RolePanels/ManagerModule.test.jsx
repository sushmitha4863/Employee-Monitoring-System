import { managerMenu } from "./managerNavigation";

test("Manager sidebar contains only the approved modules",()=>{
  expect(managerMenu.map(x=>x.label)).toEqual(["Dashboard","My Team","Projects & Tasks","Team Attendance","Leave Requests","Timesheets","Performance","Calendar","Reports","Settings"]);
});

test("Manager sidebar exposes no employee-account or disconnected pages",()=>{
  const labels=managerMenu.map(x=>x.label.join?.(" ")||x.label).join(" ");
  ["Add Employee","Create Employee","Documents","Announcements","Meetings","Profile","Notifications","Payroll","Audit Logs"].forEach(x=>expect(labels).not.toContain(x));
});