#### 获取当前设备类型 ####

	import ResizeMixin from '@/layout/mixin/ResizeHandler'

	mixins: [ResizeMixin]

	区分是否手机端：$store.state.app.device === 'mobile'

#### 回退到上一页 ####

	$router.go(-1)

	考虑默认返回：

    back() {
      if (this.$route.query.noGoBack) {
        this.$router.push({ path: '/dashboard' })
      } else {
        this.$router.go(-1)
      }
    }