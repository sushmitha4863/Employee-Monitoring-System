import { useCallback, useEffect, useMemo, useState } from "react";
import PageLayout from "../../components/layout/PageLayout";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import useEmsResource from "../../hooks/useEmsResource";
import { emsApi } from "../../services/emsApi";
import { ProfileSettingsSections } from "../Profile/Profile";
import "../SharedAdminPages.css";

const hiddenKey=/secret|password$|connection|string|credential|api.?key|jwt|smtp/i;
const label=value=>String(value||"").replace(/([a-z])([A-Z])/g,"$1 $2").replace(/[_-]/g," ");
const definitions=[
 {key:"CompanyName",category:"Company",displayName:"Company name",defaultValue:""},
 {key:"ApplicationName",category:"Company",displayName:"Application name",defaultValue:"Employee Monitoring System"},
 {key:"CompanyTimezone",category:"Company",displayName:"Company timezone",defaultValue:"Asia/Kolkata"},
 {key:"WorkingDays",category:"Working Hours",displayName:"Working days",defaultValue:"Monday,Tuesday,Wednesday,Thursday,Friday"},
 {key:"StandardWorkHours",category:"Working Hours",displayName:"Working hours",defaultValue:"8",type:"number"},
 {key:"AttendanceLateAfter",category:"Attendance Rules",displayName:"Attendance late after",defaultValue:"09:15"},
 {key:"MinimumPasswordLength",category:"Password Policy",displayName:"Minimum password length",defaultValue:"8",type:"number"},
 {key:"AccountLockoutThreshold",category:"Password Policy",displayName:"Account lockout threshold",defaultValue:"5",type:"number"},
 {key:"MfaEnabled",category:"MFA & OTP",displayName:"MFA enabled",defaultValue:"true",boolean:true},
 {key:"MfaRequiredForAdmin",category:"MFA & OTP",displayName:"MFA required for Admin",defaultValue:"true",boolean:true},
 {key:"MfaRequiredForHR",category:"MFA & OTP",displayName:"MFA required for HR",defaultValue:"true",boolean:true},
 {key:"MfaRequiredForManager",category:"MFA & OTP",displayName:"MFA required for Manager",defaultValue:"false",boolean:true},
 {key:"MfaRequiredForEmployee",category:"MFA & OTP",displayName:"MFA required for Employee",defaultValue:"false",boolean:true},
 {key:"OtpLength",category:"MFA & OTP",displayName:"OTP length",defaultValue:"6",type:"number"},
 {key:"OtpExpiryMinutes",category:"MFA & OTP",displayName:"OTP expiry (minutes)",defaultValue:"5",type:"number"},
 {key:"MaximumOtpAttempts",category:"MFA & OTP",displayName:"Maximum OTP attempts",defaultValue:"5",type:"number"},
 {key:"OtpResendCooldownSeconds",category:"MFA & OTP",displayName:"Resend cooldown (seconds)",defaultValue:"60",type:"number"},
 {key:"NotificationPreferences",category:"Notifications",displayName:"Notification configuration",defaultValue:"InApp"},
];
const categoryOrder=["Company","Working Hours","Attendance Rules","Password Policy","MFA & OTP","Notifications"];
const mergeSettings=data=>{
 const rows=(Array.isArray(data)?data:data?.settings||[]).filter(item=>!hiddenKey.test(item.settingKey||item.key));
 const byKey=new Map(rows.map(item=>[item.settingKey||item.key,item]));
 const required=definitions.map(definition=>{const existing=byKey.get(definition.key);byKey.delete(definition.key);return {...existing,...definition,settingKey:definition.key,settingValue:existing?.settingValue??existing?.value??definition.defaultValue};});
 return [...required,...byKey.values()];
};

export default function Settings(){
 const load=useCallback(()=>emsApi.settings(),[]); const {data,loading,error,reload}=useEmsResource(load); const [settings,setSettings]=useState([]); const [saving,setSaving]=useState(false); const [message,setMessage]=useState("");
 useEffect(()=>{setSettings(mergeSettings(data));},[data]);
 const grouped=useMemo(()=>settings.reduce((out,item)=>{const category=item.category||"System";(out[category]??=[]).push(item);return out;},{}),[settings]);
 const orderedGroups=useMemo(()=>Object.entries(grouped).sort(([left],[right])=>{const a=categoryOrder.indexOf(left),b=categoryOrder.indexOf(right);return (a<0?999:a)-(b<0?999:b);}),[grouped]);
 const update=(key,value)=>setSettings(rows=>rows.map(item=>(item.settingKey||item.key)===key?{...item,settingValue:value,value}:item));
 const save=async()=>{setSaving(true);setMessage("");try{await emsApi.saveSettings(settings.map(({settingKey,key,settingValue,value})=>({settingKey:settingKey||key,settingValue:settingValue??value??""})));setMessage("System settings saved and submitted for audit.");await reload();}catch(err){setMessage(err.response?.data?.message||"Unable to save settings.");}finally{setSaving(false);}};
 return <PageLayout>
  <section className="admin-page-header"><div><span className="admin-page-kicker">Administration</span><h1>Settings</h1><p>Manage system configuration, your Admin profile, and account security in one place.</p></div></section>
  <section className="admin-settings-section" aria-labelledby="system-settings-heading">
   <div className="admin-section-heading"><span>1</span><div><h2 id="system-settings-heading">System Settings</h2><p>Company, attendance, password, MFA, OTP and notification configuration.</p></div><button className="admin-primary-btn" disabled={saving||loading||Boolean(error)||settings.length===0} onClick={save}>{saving?"Saving...":"Save System Settings"}</button></div>
   <p className="admin-muted">Provider credentials, JWT secrets, database connections and other secrets are intentionally never rendered here.</p>
   {message&&<p className="admin-inline-message">{message}</p>}
   {loading&&<LoadingState message="Loading system settings..."/>}
   {!loading&&error&&<><ErrorState message={error}/><button className="admin-primary-btn" onClick={reload}>Retry</button></>}
   {!loading&&!error&&settings.length===0&&<EmptyState message="No configurable settings were returned by the backend."/>}
   {!loading&&!error&&settings.length>0&&<div className="admin-grid-2 admin-section-gap">{orderedGroups.map(([category,items])=><div className="admin-card" key={category}><h3>{label(category)}</h3><div className="admin-form-grid admin-section-gap">{items.map(item=>{const key=item.settingKey||item.key;const value=item.settingValue??item.value??"";const definition=definitions.find(entry=>entry.key===key);const boolean=definition?.boolean||typeof value==="boolean"||value==="true"||value==="false";return <div className="admin-field" key={key}><label>{item.displayName||label(key)}</label>{boolean?<select value={String(value)} onChange={event=>update(key,event.target.value)}><option value="true">Enabled</option><option value="false">Disabled</option></select>:<input type={definition?.type||(/hours|threshold|length|expiry|attempt|cooldown|timeout/i.test(key)?"number":"text")} value={value} onChange={event=>update(key,event.target.value)} required={item.isRequired}/>} {item.description&&<small className="admin-muted">{item.description}</small>}</div>;})}</div></div>)}</div>}
  </section>
  <ProfileSettingsSections/>
 </PageLayout>;
}
