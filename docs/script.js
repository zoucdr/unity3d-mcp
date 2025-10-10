// Smooth scroll for navigation links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

// Navbar scroll effect
let lastScroll = 0;
const navbar = document.querySelector('.navbar');

window.addEventListener('scroll', () => {
    const currentScroll = window.pageYOffset;
    
    if (currentScroll <= 0) {
        navbar.style.transform = 'translateY(0)';
        return;
    }
    
    if (currentScroll > lastScroll && currentScroll > 100) {
        // Scrolling down
        navbar.style.transform = 'translateY(-100%)';
    } else {
        // Scrolling up
        navbar.style.transform = 'translateY(0)';
    }
    
    lastScroll = currentScroll;
});

// Mobile menu toggle
const mobileMenuToggle = document.querySelector('.mobile-menu-toggle');
const navLinks = document.querySelector('.nav-links');

if (mobileMenuToggle) {
    mobileMenuToggle.addEventListener('click', () => {
        navLinks.classList.toggle('active');
        mobileMenuToggle.classList.toggle('active');
    });
}

// Intersection Observer for fade-in animations
const observerOptions = {
    threshold: 0.1,
    rootMargin: '0px 0px -100px 0px'
};

const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.style.opacity = '1';
            entry.target.style.transform = 'translateY(0)';
        }
    });
}, observerOptions);

// Observe all cards and sections
document.querySelectorAll('.feature-card, .scenario-card, .arch-layer, .tool-category').forEach(el => {
    el.style.opacity = '0';
    el.style.transform = 'translateY(30px)';
    el.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
    observer.observe(el);
});

// Initialize hero code with syntax highlighting
const heroCode = document.getElementById('hero-code');
if (heroCode) {
    // Create code content with proper HTML elements
    const codeLines = [
        { type: 'comment', text: '# 通过自然语言控制Unity' },
        { type: 'string', text: '"创建一个名为Player的Cube对象"' },
        { type: 'plain', text: '' },
        { type: 'comment', text: '# 批量生成游戏纹理' },
        { type: 'mixed', html: '<span class="keyword">from</span> diffusers <span class="keyword">import</span> StableDiffusionPipeline' },
        { type: 'mixed', html: 'pipe = StableDiffusionPipeline.from_pretrained(<span class="string">"model"</span>)' },
        { type: 'mixed', html: 'pipe.generate(<span class="string">"fantasy ground texture"</span>)' },
        { type: 'plain', text: '' },
        { type: 'comment', text: '# 性能分析' },
        { type: 'mixed', html: '<span class="keyword">code_runner</span>: PerformanceAnalyzer.Analyze()' },
        { type: 'plain', text: '' },
        { type: 'output', text: '✓ GameObject创建成功' },
        { type: 'output', text: '✓ 10张纹理已生成' },
        { type: 'output', text: '✓ 场景性能报告已生成' }
    ];
    
    // Build HTML
    let htmlContent = '';
    codeLines.forEach(line => {
        if (line.type === 'mixed') {
            htmlContent += line.html + '\n';
        } else if (line.type === 'plain') {
            htmlContent += '\n';
        } else {
            htmlContent += `<span class="${line.type}">${line.text}</span>\n`;
        }
    });
    
    // Typing animation for desktop
    if (window.innerWidth > 768) {
        heroCode.innerHTML = '';
        let charIndex = 0;
        
        const typeWriter = () => {
            if (charIndex < htmlContent.length) {
                const char = htmlContent[charIndex];
                
                // Handle HTML tags specially
                if (char === '<') {
                    const closingIndex = htmlContent.indexOf('>', charIndex);
                    if (closingIndex !== -1) {
                        heroCode.innerHTML += htmlContent.substring(charIndex, closingIndex + 1);
                        charIndex = closingIndex + 1;
                    } else {
                        heroCode.innerHTML += char;
                        charIndex++;
                    }
                } else {
                    heroCode.innerHTML += char;
                    charIndex++;
                }
                
                setTimeout(typeWriter, 3);
            }
        };
        
        setTimeout(typeWriter, 800);
    } else {
        // Mobile: show immediately
        heroCode.innerHTML = htmlContent;
    }
}

// Add parallax effect to hero section
window.addEventListener('scroll', () => {
    const scrolled = window.pageYOffset;
    const heroVisual = document.querySelector('.hero-visual');
    
    if (heroVisual) {
        heroVisual.style.transform = `translateY(${scrolled * 0.3}px)`;
    }
});

// Dynamic gradient background
const gradientBg = document.querySelector('.gradient-bg');
if (gradientBg) {
    document.addEventListener('mousemove', (e) => {
        const x = e.clientX / window.innerWidth;
        const y = e.clientY / window.innerHeight;
        
        gradientBg.style.transform = `translate(${x * 30}px, ${y * 30}px)`;
    });
}

// Copy code to clipboard functionality
document.querySelectorAll('.code-block').forEach(block => {
    const copyButton = document.createElement('button');
    copyButton.className = 'copy-btn';
    copyButton.innerHTML = `
        <svg width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
            <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
            <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
        </svg>
    `;
    
    copyButton.addEventListener('click', () => {
        const code = block.querySelector('code').textContent;
        navigator.clipboard.writeText(code).then(() => {
            copyButton.innerHTML = `
                <svg width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                    <polyline points="20 6 9 17 4 12"></polyline>
                </svg>
            `;
            setTimeout(() => {
                copyButton.innerHTML = `
                    <svg width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                        <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                        <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                    </svg>
                `;
            }, 2000);
        });
    });
    
    block.style.position = 'relative';
    block.appendChild(copyButton);
});

// Add copy button styles
const style = document.createElement('style');
style.textContent = `
    .copy-btn {
        position: absolute;
        top: 1rem;
        right: 1rem;
        padding: 0.5rem;
        background: rgba(99, 102, 241, 0.1);
        border: 1px solid rgba(99, 102, 241, 0.3);
        border-radius: 0.5rem;
        color: var(--text-primary);
        cursor: pointer;
        transition: all 0.3s;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    
    .copy-btn:hover {
        background: rgba(99, 102, 241, 0.2);
    }
    
    @media (max-width: 768px) {
        .nav-links.active {
            display: flex;
            flex-direction: column;
            position: absolute;
            top: 100%;
            left: 0;
            right: 0;
            background: rgba(15, 23, 42, 0.95);
            backdrop-filter: blur(10px);
            padding: 1rem;
            border-bottom: 1px solid var(--border-color);
            gap: 1rem;
        }
        
        .mobile-menu-toggle.active span:nth-child(1) {
            transform: rotate(45deg) translate(5px, 5px);
        }
        
        .mobile-menu-toggle.active span:nth-child(2) {
            opacity: 0;
        }
        
        .mobile-menu-toggle.active span:nth-child(3) {
            transform: rotate(-45deg) translate(5px, -5px);
        }
    }
`;
document.head.appendChild(style);

// Add loading animation
window.addEventListener('load', () => {
    document.body.style.opacity = '0';
    setTimeout(() => {
        document.body.style.transition = 'opacity 0.5s ease';
        document.body.style.opacity = '1';
    }, 100);
});

// Stats counter animation
const animateCounter = (element, target, duration = 2000) => {
    let start = 0;
    const increment = target / (duration / 16);
    
    const timer = setInterval(() => {
        start += increment;
        if (start >= target) {
            element.textContent = target + (element.textContent.includes('+') ? '+' : '');
            clearInterval(timer);
        } else {
            element.textContent = Math.floor(start) + (element.textContent.includes('+') ? '+' : '');
        }
    }, 16);
};

// Observe stats section
const statsObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            const statNumbers = entry.target.querySelectorAll('.stat-number');
            statNumbers.forEach(stat => {
                const target = parseInt(stat.textContent);
                if (!isNaN(target)) {
                    animateCounter(stat, target);
                }
            });
            statsObserver.unobserve(entry.target);
        }
    });
}, { threshold: 0.5 });

const heroStats = document.querySelector('.hero-stats');
if (heroStats) {
    statsObserver.observe(heroStats);
}

console.log('%c Unity3d MCP ', 'background: linear-gradient(135deg, #6366f1, #8b5cf6); color: white; font-size: 20px; padding: 10px; border-radius: 5px;');
console.log('%c AI驱动的Unity开发工作流 ', 'color: #6366f1; font-size: 14px;');
console.log('%c GitHub: https://github.com/yourusername/unity3d-mcp ', 'color: #64748b; font-size: 12px;');

