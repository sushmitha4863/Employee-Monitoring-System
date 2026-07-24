import { useEffect, useState } from "react";
import { ErrorState, LoadingState } from "../../components/common/ResourceStates";
import ProfileImageUploader from "../../components/common/ProfileImageUploader";
import { emsApi } from "../../services/emsApi";
import "../SharedAdminPages.css";

export const ProfileSettingsSections = () => {
  const [profile, setProfile] = useState(null);
  const [form, setForm] = useState({ phoneNumber: "", address: "", themePreference: "", notificationPreference: "" });
  const [password, setPassword] = useState({ oldPassword: "", newPassword: "", confirmPassword: "" });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    emsApi.profile().then((data) => {
      setProfile(data);
      setForm({ phoneNumber: data?.phoneNumber || "", address: data?.address || "", themePreference: data?.themePreference || "", notificationPreference: data?.notificationPreference || "" });
    }).catch((err) => setError(err.response?.data?.message || "Unable to load profile.")).finally(() => setLoading(false));
  }, []);

  const updateProfile = async (event) => {
    event.preventDefault(); setSaving(true); setMessage("");
    try { await emsApi.updateProfile(form); setMessage("Profile details updated."); }
    catch (err) { setMessage(err.response?.data?.message || "Unable to update profile."); }
    finally { setSaving(false); }
  };

  const updatePassword = async (event) => {
    event.preventDefault(); setSaving(true); setMessage("");
    if (password.newPassword.length < 8) { setMessage("New password must be at least 8 characters."); setSaving(false); return; }
    if (password.newPassword !== password.confirmPassword) { setMessage("Password confirmation does not match."); setSaving(false); return; }
    if (password.newPassword === password.oldPassword) { setMessage("New password must differ from the current password."); setSaving(false); return; }
    try {
      await emsApi.changePassword({ oldPassword: password.oldPassword, newPassword: password.newPassword, confirmNewPassword: password.confirmPassword });
      setPassword({ oldPassword: "", newPassword: "", confirmPassword: "" });
      setMessage("Password changed successfully.");
    } catch (err) { setMessage(err.response?.data?.message || "Unable to change password."); }
    finally { setSaving(false); }
  };

  if (loading) return <LoadingState message="Loading profile and security settings..." />;
  if (error) return <ErrorState message={error} />;
  if (!profile) return null;

  return <>
    {message && <p className="admin-inline-message">{message}</p>}
    <section className="admin-settings-section" aria-labelledby="my-profile-heading">
      <div className="admin-section-heading"><span>2</span><div><h2 id="my-profile-heading">My Profile</h2><p>Manage permitted personal details and preferences. Official identity fields remain read-only.</p></div></div>
      <div className="admin-card">
        <ProfileImageUploader initial={profile.profileImage} name={profile.fullName || "Admin"} />
        <div className="admin-profile-identity">
          <div className="admin-field"><label>Display name</label><input value={profile.fullName || ""} readOnly aria-readonly="true" /></div>
          <div className="admin-field"><label>Username</label><input value={profile.username || ""} readOnly aria-readonly="true" /></div>
          <div className="admin-field"><label>Official email</label><input value={profile.email || ""} readOnly aria-readonly="true" /></div>
          <div className="admin-field"><label>Role</label><input value={profile.role || ""} readOnly aria-readonly="true" /></div>
          <div className="admin-field"><label>Admin code</label><input value={profile.adminCode || profile.employeeCode || "Not assigned"} readOnly aria-readonly="true" /></div>
        </div>
        <form className="admin-form-grid admin-section-gap" onSubmit={updateProfile}>
          <div className="admin-field"><label>Phone number</label><input value={form.phoneNumber} onChange={(event) => setForm({ ...form, phoneNumber: event.target.value })} /></div>
          <div className="admin-field"><label>Address</label><input value={form.address} onChange={(event) => setForm({ ...form, address: event.target.value })} /></div>
          <div className="admin-field"><label>Theme preference</label><select value={form.themePreference} onChange={(event) => setForm({ ...form, themePreference: event.target.value })}><option value="">System default</option><option>Light</option><option>Dark</option></select></div>
          <div className="admin-field"><label>Notification preference</label><select value={form.notificationPreference} onChange={(event) => setForm({ ...form, notificationPreference: event.target.value })}><option value="">System default</option><option>All</option><option>ImportantOnly</option><option>None</option></select></div>
          <button className="admin-soft-btn" type="submit" disabled={saving}>Save Profile</button>
        </form>
      </div>
    </section>
    <section className="admin-settings-section" aria-labelledby="security-password-heading">
      <div className="admin-section-heading"><span>3</span><div><h2 id="security-password-heading">Security &amp; Password</h2><p>Review MFA status and update your password using the existing security policy.</p></div></div>
      <div className="admin-card">
        <div className="admin-security-summary"><div><span>MFA status</span><strong>{profile.mfaEnabled ? "Enabled" : "Required by Admin policy"}</strong></div><div><span>Last password change</span><strong>{profile.lastPasswordChangedAt ? new Date(profile.lastPasswordChangedAt).toLocaleString() : "Not available"}</strong></div></div>
        <form className="admin-form-grid admin-section-gap" onSubmit={updatePassword}>
          <div className="admin-field"><label>Current password</label><input type="password" autoComplete="current-password" value={password.oldPassword} onChange={(event) => setPassword({ ...password, oldPassword: event.target.value })} required /></div>
          <div className="admin-field"><label>New password</label><input type="password" autoComplete="new-password" value={password.newPassword} onChange={(event) => setPassword({ ...password, newPassword: event.target.value })} minLength={8} required /></div>
          <div className="admin-field"><label>Confirm new password</label><input type="password" autoComplete="new-password" value={password.confirmPassword} onChange={(event) => setPassword({ ...password, confirmPassword: event.target.value })} minLength={8} required /></div>
          <button className="admin-primary-btn" type="submit" disabled={saving}>Change Password</button>
        </form>
      </div>
    </section>
  </>;
};

export default ProfileSettingsSections;