import { useEffect, useState } from "react";
import {
  AppWindow,
  ArrowDownToLine,
  ChevronRight,
  Code2,
  Gamepad2,
  Keyboard,
  Menu,
  Mic2,
  MonitorCog,
  MousePointer2,
  Palette,
  ShieldCheck,
  Volume2,
  X,
  Zap,
} from "lucide-react";

const DOWNLOAD_PATH = "/downloads/PulseMute.exe";

const gallery = [
  { image: "/screenshots/screen-1.png", label: "Main control", detail: "Mute state, device, and dual hotkeys at a glance." },
  { image: "/screenshots/screen-2.png", label: "General", detail: "Startup, placement, and taskbar behavior." },
  { image: "/screenshots/screen-3.png", label: "Hotkeys", detail: "Two independently assignable input slots." },
  { image: "/screenshots/screen-4.png", label: "Audio", detail: "Microphone, feedback style, and volume." },
  { image: "/screenshots/screen-5.png", label: "Controller", detail: "DualSense and DualSense Edge support." },
  { image: "/screenshots/screen-6.png", label: "Customization", detail: "Theme, interface colors, logo, and mute control." },
];

const features = [
  { icon: Keyboard, title: "Global hotkeys", text: "Assign one or two keyboard keys and toggle your microphone from anywhere." },
  { icon: Gamepad2, title: "DualSense ready", text: "Use the Mute, PS, touchpad, paddles, triggers, or any supported controller button." },
  { icon: MousePointer2, title: "Full mouse input", text: "Mouse buttons, side buttons, and vertical or horizontal wheel directions can be assigned." },
  { icon: AppWindow, title: "Tray and placement", text: "Keep PulseMute in the tray, pin it above other windows, and restore its exact position." },
  { icon: Volume2, title: "Audio confirmation", text: "Choose from nine feedback styles and set exactly how loud confirmation should be." },
  { icon: Palette, title: "Made to be yours", text: "Customize themes, colors, logos, mute controls, and compact layout behavior." },
];

function App() {
  const [menuOpen, setMenuOpen] = useState(false);
  const [activeShot, setActiveShot] = useState(0);

  useEffect(() => {
    const closeMenu = () => setMenuOpen(false);
    window.addEventListener("resize", closeMenu);
    return () => window.removeEventListener("resize", closeMenu);
  }, []);

  return (
    <main>
      <header className="site-header">
        <a className="brand" href="#top" aria-label="PulseMute home">
          <img src="/assets/pulsemute-logo.svg" alt="" />
          <span>PulseMute</span>
        </a>
        <nav className={menuOpen ? "nav-links is-open" : "nav-links"} aria-label="Main navigation">
          <a href="#features" onClick={() => setMenuOpen(false)}>Features</a>
          <a href="#interface" onClick={() => setMenuOpen(false)}>Interface</a>
          <a href="#details" onClick={() => setMenuOpen(false)}>Details</a>
          <a href="https://github.com/w-rakeeb/PulseMute" target="_blank" rel="noreferrer">GitHub</a>
        </nav>
        <a className="header-download" href={DOWNLOAD_PATH} download>
          <ArrowDownToLine size={17} /> Download
        </a>
        <button className="menu-button" onClick={() => setMenuOpen(!menuOpen)} aria-label={menuOpen ? "Close menu" : "Open menu"}>
          {menuOpen ? <X /> : <Menu />}
        </button>
      </header>

      <section className="hero" id="top">
        <div className="hero-copy">
          <div className="release-tag"><span /> Version 26.1.0.9-stable for Windows</div>
          <h1>PulseMute</h1>
          <p className="hero-lead">Fast, quiet microphone control built around the way you actually play, talk, and work.</p>
          <div className="hero-actions">
            <a className="primary-button" href={DOWNLOAD_PATH} download>
              <ArrowDownToLine size={20} /> Download for Windows
            </a>
            <a className="text-link" href="#interface">Explore the interface <ChevronRight size={17} /></a>
          </div>
          <div className="hero-meta">
            <span><ShieldCheck size={16} /> Portable executable</span>
            <span><Zap size={16} /> Instant global control</span>
          </div>
        </div>

        <div className="product-stage" aria-label="PulseMute application preview">
          <div className="stage-label"><span className="live-dot" /> Live microphone state</div>
          <img className="app-window" src="/screenshots/screen-1.png" alt="PulseMute compact microphone control window" />
          <div className="input-chip keyboard-chip"><Keyboard size={18} /><span>Keyboard</span></div>
          <div className="input-chip controller-chip"><Gamepad2 size={18} /><span>DualSense</span></div>
          <div className="input-chip mouse-chip"><MousePointer2 size={18} /><span>Mouse</span></div>
        </div>
      </section>

      <section className="proof-strip" aria-label="Product summary">
        <div><strong>2</strong><span>Hotkey slots</span></div>
        <div><strong>9</strong><span>Feedback sounds</span></div>
        <div><strong>3</strong><span>Input families</span></div>
        <div><strong>1</strong><span>Compact control hub</span></div>
      </section>

      <section className="section features-section" id="features">
        <div className="section-heading">
          <span className="eyebrow">Control without friction</span>
          <h2>One mute switch. Every input you use.</h2>
          <p>PulseMute keeps the common actions close and the deeper controls organized.</p>
        </div>
        <div className="feature-grid">
          {features.map(({ icon: Icon, title, text }) => (
            <article className="feature-card" key={title}>
              <div className="feature-icon"><Icon size={21} /></div>
              <h3>{title}</h3>
              <p>{text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="section interface-section" id="interface">
        <div className="section-heading interface-heading">
          <span className="eyebrow">The complete interface</span>
          <h2>Compact in front. Capable underneath.</h2>
          <p>The control window stays small on a second monitor while Settings gives each job a focused place.</p>
        </div>
        <div className="gallery-layout">
          <div className="gallery-tabs" role="tablist" aria-label="PulseMute interface views">
            {gallery.map((item, index) => (
              <button
                key={item.label}
                className={activeShot === index ? "gallery-tab is-active" : "gallery-tab"}
                onClick={() => setActiveShot(index)}
                role="tab"
                aria-selected={activeShot === index}
              >
                <span>{String(index + 1).padStart(2, "0")}</span>
                <strong>{item.label}</strong>
                <ChevronRight size={17} />
              </button>
            ))}
          </div>
          <div className="gallery-view" role="tabpanel">
            <div className="gallery-bar">
              <span>{gallery[activeShot].label}</span>
              <span className="gallery-state"><i /> PulseMute interface</span>
            </div>
            <div className="gallery-canvas">
              <img src={gallery[activeShot].image} alt={`PulseMute ${gallery[activeShot].label} interface`} />
            </div>
            <p>{gallery[activeShot].detail}</p>
          </div>
        </div>
      </section>

      <section className="section details-section" id="details">
        <div className="details-copy">
          <span className="eyebrow">Built for Windows</span>
          <h2>Small footprint. Serious control.</h2>
          <p>Run it from the tray, pin it above other windows, remember its exact placement, or let it start with Windows. Each release keeps its own settings and instance identity.</p>
          <ul>
            <li><ShieldCheck size={18} /> Isolated settings and startup identity</li>
            <li><MonitorCog size={18} /> Responsive compact window sizing</li>
            <li><Mic2 size={18} /> Selectable Windows capture device</li>
            <li><AppWindow size={18} /> Tray mode and stay-on-top control</li>
          </ul>
        </div>
        <div className="state-showcase">
          <div className="state-column muted-state">
            <img src="/assets/control-muted.png" alt="Muted microphone control" />
            <span>Muted</span>
            <p>Clear red state at a glance.</p>
          </div>
          <div className="state-column live-state">
            <img src="/assets/control-live.png" alt="Live microphone control" />
            <span>Live</span>
            <p>Immediate green confirmation.</p>
          </div>
        </div>
      </section>

      <section className="download-section">
        <img src="/assets/pulsemute-logo.svg" alt="" />
        <span className="eyebrow">PulseMute 26.1.0.9-stable</span>
        <h2>Your microphone, on your terms.</h2>
        <p>Download the portable Windows build and choose the shortcut that fits your setup.</p>
        <a className="primary-button" href={DOWNLOAD_PATH} download>
          <ArrowDownToLine size={20} /> Download PulseMute
        </a>
      </section>

      <footer>
        <a className="brand footer-brand" href="#top"><img src="/assets/pulsemute-logo.svg" alt="" /><span>PulseMute</span></a>
        <p>Designed and built by Wrakeeb.</p>
        <a href="https://github.com/w-rakeeb/PulseMute" target="_blank" rel="noreferrer"><Code2 size={18} /> GitHub</a>
      </footer>
    </main>
  );
}

export default App;
