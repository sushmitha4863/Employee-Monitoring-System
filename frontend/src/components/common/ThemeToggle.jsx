import { MdDarkMode, MdLightMode } from "react-icons/md";
import { useTheme } from "../../context/ThemeContext";
export default function ThemeToggle({onChange}){const {theme,setTheme}=useTheme();const toggle=()=>{const next=theme==='dark'?'light':'dark';setTheme(next);onChange?.(next)};return <button className="theme-toggle" aria-label={`Switch to ${theme==='dark'?'light':'dark'} theme`} title={`Switch to ${theme==='dark'?'light':'dark'} theme`} onClick={toggle}>{theme==='dark'?<MdLightMode/>:<MdDarkMode/>}</button>}

