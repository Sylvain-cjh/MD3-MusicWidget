#🎵 MD3 Music Widget 
	可能是 Windows 上最好看的音乐控件（划掉）
使用Google极为先进的Material Design 3设计语言，实现优秀的视觉效果以及出色的ui设计。
量子级色彩引擎 (Dynamic Tonal Palette)
实时提取当前播放音乐封面的主色调。基于自研（？）的 `Clamp` 极值算法，浅色模式清爽通透，深色模式深邃高级（并未完全实现）。
优秀的物理动效 (Physics-based Motion)
全面引入 `ElasticEase` (弹性缓动)、`QuarticEase` (四次缓动) 和 `BackEase` (回弹缓动)。播放控件以灵动的物理效果增加反馈。
背景使用渐变 (Radial Gradient Fade)
剔除了传统的暴力裁剪，采用底层 `RadialGradientBrush` 与高斯模糊融合。封面如晨光般向外辐射绽放，伴随精密的 5 级非线性透明度断点，彻底消灭（可能）Windows 祖传的色彩断层 。
极致的性能优化 (Zero-Lag Performance)
硬件级 `BitmapCache` 纹理冻结 + 内存级 `Freeze()` 剥离 + 图像实时下采样，把高斯模糊带来的 CPU/GPU 占用压缩到极致，内存占用极低。
点点Star⭐️喵，点点Star⭐️谢谢喵。
