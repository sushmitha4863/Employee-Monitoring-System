import EmployeeLayout from "./EmployeeLayout";
export const EmployeeHero=({title="Employee Self Service",subtitle="Manage your personal work.",action})=><section className="employee-hero"><div><h1>{title}</h1><p>{subtitle}</p></div>{action&&<div className="employee-hero-action">{action}</div>}</section>;
export const DashboardStats=()=>null;
export {EmployeeLayout};
