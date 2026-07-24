import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
const ThemeContext=createContext(null);
const normalize=value=>String(value||'').toLowerCase()==='dark'?'dark':'light';
export function ThemeProvider({children}){const [theme,setValue]=useState(()=>normalize(localStorage.getItem('ems-theme')));const setTheme=useCallback(value=>setValue(normalize(value)),[]);const toggleTheme=useCallback(()=>setValue(x=>x==='dark'?'light':'dark'),[]);useEffect(()=>{document.documentElement.dataset.theme=theme;document.documentElement.style.colorScheme=theme;localStorage.setItem('ems-theme',theme);},[theme]);const value=useMemo(()=>({theme,setTheme,toggleTheme}),[theme,setTheme,toggleTheme]);return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>}
export function useTheme(){const value=useContext(ThemeContext);if(!value)throw new Error('useTheme must be used inside ThemeProvider');return value}

