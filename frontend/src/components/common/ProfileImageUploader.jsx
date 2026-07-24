import { useState } from "react";
import { emsApi } from "../../services/emsApi";
import "./ProfileImageUploader.css";

const ProfileImageUploader = ({ initial, name = "User", onUploaded }) => {
  const [preview, setPreview] = useState(initial || "");
  const [message, setMessage] = useState("");

  const handleFile = async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setPreview(URL.createObjectURL(file));
    setMessage("Uploading...");

    try {
      const response = await emsApi.uploadProfileImage(file);
      const imageUrl = response.data?.imageUrl;
      setMessage("Profile photo updated");
      onUploaded?.(imageUrl);
    } catch (error) {
      setMessage(error.response?.data?.message || "Upload failed. Please login and try again.");
    }
  };

  return (
    <div className="profile-uploader">
      <div className="profile-photo-preview">
        {preview ? <img src={preview} alt={name} /> : <span>{name[0]}</span>}
      </div>
      <label className="profile-upload-btn">
        Upload Photo
        <input type="file" accept="image/*" onChange={handleFile} />
      </label>
      {message && <small>{message}</small>}
    </div>
  );
};

export default ProfileImageUploader;